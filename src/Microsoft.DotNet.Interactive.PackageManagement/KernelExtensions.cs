// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.PackageManagement;

public static class KernelExtensions
{
    public static T UseNugetDirective<T>(this T kernel, Func<T, IReadOnlyList<ResolvedPackageReference>, Task> onResolvePackageReferences, bool forceRestore = false)
        where T : Kernel
    {
        var lazyPackageRestoreContext = new Lazy<PackageRestoreContext>(() =>
        {
            var packageRestoreContext = new PackageRestoreContext(forceRestore);

            kernel.RegisterForDisposal(packageRestoreContext);

            return packageRestoreContext;
        });

        AddRDirective();
        AddIDirective();
        AddRestoreDirective();

        return kernel;

        void AddRDirective()
        {
            var poundRDirective = new KernelActionDirective("#r")
            {
                Description = """Add a NuGet package reference using #r "nuget:<package>[,<version>]" or reference an assembly using #r "<path to assembly>" """,
                Parameters =
                {
                    new("")
                    {
                        AllowImplicitName = true,
                        Required = true
                    }
                },
                TryGetKernelCommandAsync = AddPackage.TryParseRDirectiveAsync
            };

            kernel.AddDirective<AddPackage>(
                poundRDirective,
                (command, context) =>
                {
                    HandleAddPackageReference(
                        context,
                        lazyPackageRestoreContext,
                        new PackageReference(command.PackageName, command.PackageVersion));

                    return Task.CompletedTask;
                });
        }

        void AddIDirective()
        {
            var directive = new KernelActionDirective("#i")
            {
                Description = "Include a NuGet package source or search path for referenced assemblies",
                Parameters =
                {
                    new("")
                    {
                        AllowImplicitName = true,
                        Required = true
                    }
                },
                TryGetKernelCommandAsync = AddPackageSource.TryParseIDirectiveAsync
            };

            kernel.AddDirective<AddPackageSource>(
                directive,
                (command, context) =>
                {
                    lazyPackageRestoreContext.Value.TryAddRestoreSource(command.PackageSource);
                    return Task.CompletedTask;
                });
        }

        void AddRestoreDirective()
        {
            var directive = new KernelActionDirective("#!nuget-restore")
            {
                Hidden = true
            };

            kernel.AddDirective(
                directive,
                async (_, context) =>
                {
                    var command = new AnonymousKernelCommand((_, ctx) => Restore(ctx, lazyPackageRestoreContext, onResolvePackageReferences));
                    command.SetParent(context.Command, true);
                    await context.HandlingKernel.SendAsync(command);
                }
            );
        }
    }

    private static void HandleAddPackageReference(
        KernelInvocationContext context,
        Lazy<PackageRestoreContext> packageRestoreContext,
        PackageReference pkg)
    {
        var alreadyGotten = packageRestoreContext.Value.ResolvedPackageReferences
                                                 .Concat(packageRestoreContext.Value.RequestedPackageReferences)
                                                 .FirstOrDefault(r => r.PackageName.Equals(pkg.PackageName, StringComparison.OrdinalIgnoreCase));

        if (alreadyGotten is not null && 
            !string.IsNullOrWhiteSpace(pkg.PackageVersion) && 
            pkg.PackageVersion != alreadyGotten.PackageVersion)
        {
            if (!pkg.IsPackageVersionSpecified || pkg.PackageVersion is "*-*" or "*")
            {
                // we will reuse the already loaded package since this is a wildcard
                var added = packageRestoreContext.Value.GetOrAddPackageReference(alreadyGotten.PackageName, alreadyGotten.PackageVersion);

                if (added is null)
                {
                    var errorMessage = GenerateErrorMessage(pkg);
                    context.Fail(context.Command, message: errorMessage);
                }
            }
            else
            {
                var errorMessage = GenerateErrorMessage(pkg, alreadyGotten);
                context.Fail(context.Command, message: errorMessage);
            }
        }
        else
        {
            var added = packageRestoreContext.Value.GetOrAddPackageReference(pkg.PackageName, pkg.PackageVersion);

            if (added is null)
            {
                var errorMessage = GenerateErrorMessage(pkg);
                context.Fail(context.Command, message: errorMessage);
            }
        }

        static string GenerateErrorMessage(
            PackageReference requested,
            PackageReference existing = null)
        {
            if (existing is not null)
            {
                if (!string.IsNullOrEmpty(requested.PackageName))
                {
                    if (!string.IsNullOrEmpty(requested.PackageVersion))
                    {
                        return $"{requested.PackageName} version {requested.PackageVersion} cannot be added because version {existing.PackageVersion} was added previously.";
                    }
                }
            }

            return $"Invalid Package specification: '{requested}'";
        }
    }

    private static async Task Restore<T>(KernelInvocationContext context,
        Lazy<PackageRestoreContext> lazyPackageRestoreContext, Func<T, IReadOnlyList<ResolvedPackageReference>, Task> registerResolvedPackageReferences) where T : Kernel
    {
        var requestedPackages = lazyPackageRestoreContext.Value.RequestedPackageReferences.Select(s => s.PackageName).OrderBy(s => s).ToList();

        var requestedSources = lazyPackageRestoreContext.Value.RestoreSources.OrderBy(s => s).ToList();

        var installMessage = new InstallPackagesMessage(requestedSources, requestedPackages, Array.Empty<string>(), 0);

        var displayedValue = context.Display(installMessage);
                
        var restorePackagesTask = lazyPackageRestoreContext.Value.RestoreAsync();
        var delay = 500;
        while (await Task.WhenAny(Task.Delay(delay), restorePackagesTask) != restorePackagesTask)
        {
            if (context.CancellationToken.IsCancellationRequested)
            {
                break;
            }

            installMessage.Progress++;

            displayedValue.Update(installMessage);
        }

        var result = await restorePackagesTask;

        var resultMessage = new InstallPackagesMessage(
            requestedSources,
            Array.Empty<string>(),
            lazyPackageRestoreContext.Value.ResolvedPackageReferences
                                     .Where(r => requestedPackages.Contains(r.PackageName, StringComparer.OrdinalIgnoreCase))
                                     .Select(s => $"{s.PackageName}, {s.PackageVersion}")
                                     .OrderBy(s => s)
                                     .ToList(),
            0);

        if (result.Succeeded)
        {
            await registerResolvedPackageReferences(context.HandlingKernel as T, result.ResolvedReferences);
            foreach (var resolvedReference in result.ResolvedReferences)
            {
                context.Publish(new PackageAdded(resolvedReference, context.Command));
            }

            displayedValue.Update(resultMessage);
        }
        else
        {
            var errors = string.Join(Environment.NewLine, result.Errors);
            displayedValue.Update(resultMessage);
            context.Fail(context.Command, message: errors);
        }
    }
}