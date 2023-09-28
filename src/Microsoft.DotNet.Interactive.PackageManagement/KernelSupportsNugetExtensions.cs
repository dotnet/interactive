// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive;

public static class KernelSupportsNugetExtensions
{
    public static T UseNugetDirective<T>(this T kernel, bool useResultsCache = true)
        where T : Kernel, ISupportNuget
    {
        var lazyPackageRestoreContext = new Lazy<PackageRestoreContext>(() =>
        {
            var packageRestoreContext = new PackageRestoreContext(useResultsCache);

            kernel.RegisterForDisposal(packageRestoreContext);

            return packageRestoreContext;
        });

        kernel.AddDirective(i(lazyPackageRestoreContext));
        kernel.AddDirective(r(lazyPackageRestoreContext));

        var restore = new Command("#!nuget-restore")
        {
            Handler = CommandHandler.Create((KernelCommandInvocation)(async (_, context) => await context.ScheduleAsync(c => Restore(c, lazyPackageRestoreContext)))),
            IsHidden = true
        };

        kernel.AddDirective(restore);

        return kernel;
    }

    private static Command i(Lazy<PackageRestoreContext> lazyPackageRestoreContext)
    {
        var iDirective = new Command("#i")
        {
            new Argument<string>("source")
        };

        iDirective.Handler = CommandHandler.Create<string>((source) =>
        {
            lazyPackageRestoreContext.Value.TryAddRestoreSource(source.Replace("nuget:", ""));
        });
        return iDirective;
    }

    private static Command r(Lazy<PackageRestoreContext> lazyPackageRestoreContext)
    {
        var rDirective = new Command("#r")
        {
            new Argument<PackageReferenceOrFileInfo>(
                result =>
                {
                    var token = result.Tokens
                        .Select(t => t.Value)
                        .SingleOrDefault();

                    if (PackageReference.TryParse(token, out var reference))
                    {
                        return reference;
                    }

                    if (token is not null &&
                        !token.StartsWith("nuget:") &&
                        !EndsInDirectorySeparator(token))
                    {
                        return new FileInfo(token);
                    }

                    result.ErrorMessage = $"Unable to parse package reference: \"{token}\"";

                    return null;
                })
            {
                Name = "package"
            }
        };

        rDirective.Handler = CommandHandler.Create<PackageReferenceOrFileInfo, KernelInvocationContext>((package, context) => HandleAddPackageReference(package, context, lazyPackageRestoreContext));

        return rDirective;

        Task HandleAddPackageReference(
            PackageReferenceOrFileInfo package,
            KernelInvocationContext context, 
            Lazy<PackageRestoreContext> packageRestoreContext)
        {
            if (package?.Value is PackageReference pkg &&
                context.HandlingKernel is ISupportNuget kernel)
            {
                var alreadyGotten = packageRestoreContext.Value.ResolvedPackageReferences
                    .Concat(packageRestoreContext.Value.RequestedPackageReferences)
                    .FirstOrDefault(r => r.PackageName.Equals(pkg.PackageName, StringComparison.OrdinalIgnoreCase));

                if (alreadyGotten is { } && !string.IsNullOrWhiteSpace(pkg.PackageVersion) && pkg.PackageVersion != alreadyGotten.PackageVersion)
                {
                    if (!pkg.IsPackageVersionSpecified || pkg.PackageVersion is "*-*" or "*")
                    {
                        // we will reuse the the already loaded since this is a wildcard
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

            return Task.CompletedTask;
        }
    }

    private static bool EndsInDirectorySeparator(string path)
    {
        return path.Length > 0 && path.EndsWith(Path.DirectorySeparatorChar);
    }
    
    private static async Task Restore(KernelInvocationContext context, Lazy<PackageRestoreContext> lazyPackageRestoreContext)
    {
        if (context.HandlingKernel is not ISupportNuget kernel)
        {
            return;
        }


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
            kernel.RegisterResolvedPackageReferences(result.ResolvedReferences);
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