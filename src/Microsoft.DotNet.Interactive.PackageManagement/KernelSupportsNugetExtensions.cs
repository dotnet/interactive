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
        kernel.AddDirective(i());
        kernel.AddDirective(r());

        var restore = new Command("#!nuget-restore")
        {
            Handler = CommandHandler.Create(DoNugetRestore()),
            IsHidden = true
        };
        kernel.Configure(useResultsCache);
        kernel.AddDirective(restore);

        return kernel;

        static KernelCommandInvocation DoNugetRestore() => async (_, context) => await context.ScheduleAsync(Restore);
    }

    private static Command i()
    {
        var iDirective = new Command("#i")
        {
            new Argument<string>("source")
        };
        iDirective.Handler = CommandHandler.Create<string, KernelInvocationContext>((source, context) =>
        {
            if (context.HandlingKernel is ISupportNuget kernel)
            {
                kernel.TryAddRestoreSource(source.Replace("nuget:", ""));
            }
        });
        return iDirective;
    }

    private static Command r()
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

        rDirective.Handler = CommandHandler.Create<PackageReferenceOrFileInfo, KernelInvocationContext>(HandleAddPackageReference);

        return rDirective;

        Task HandleAddPackageReference(
            PackageReferenceOrFileInfo package,
            KernelInvocationContext context)
        {
            if (package?.Value is PackageReference pkg &&
                context.HandlingKernel is ISupportNuget kernel)
            {
                var alreadyGotten = kernel.ResolvedPackageReferences
                    .Concat(kernel.RequestedPackageReferences)
                    .FirstOrDefault(r => r.PackageName.Equals(pkg.PackageName, StringComparison.OrdinalIgnoreCase));

                if (alreadyGotten is { } && !string.IsNullOrWhiteSpace(pkg.PackageVersion) && pkg.PackageVersion != alreadyGotten.PackageVersion)
                {
                    if (!pkg.IsPackageVersionSpecified || pkg.PackageVersion is "*-*" or "*")
                    {
                        // we will reuse the the already loaded since this is a wildcard
                        var added = kernel.GetOrAddPackageReference(alreadyGotten.PackageName, alreadyGotten.PackageVersion);

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
                    var added = kernel.GetOrAddPackageReference(pkg.PackageName, pkg.PackageVersion);

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
    
    private static async Task Restore(KernelInvocationContext context)
    {
        if (context.HandlingKernel is not ISupportNuget kernel)
        {
            return;
        }

        var requestedPackages = kernel.RequestedPackageReferences.Select(s => s.PackageName).OrderBy(s => s).ToList();

        var requestedSources = kernel.RestoreSources.OrderBy(s => s).ToList();

        var installMessage = new InstallPackagesMessage(requestedSources, requestedPackages, Array.Empty<string>(), 0);

        var displayedValue = context.Display(installMessage);
                
        var restorePackagesTask = kernel.RestoreAsync();
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
            kernel.ResolvedPackageReferences
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