// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Utility;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.CSharp
{
    public static class CSharpKernelExtensions
    {
        public static CSharpKernel UseDefaultFormatting(
            this CSharpKernel kernel)
        {
            var command = new SubmitCode($@"
using static {typeof(PocketViewTags).FullName};
using {typeof(PocketView).Namespace};
");

            kernel.DeferCommand(command);

            return kernel;
        }

        public static CSharpKernel UseKernelHelpers(
            this CSharpKernel kernel)
        {
            var command = new SubmitCode($@"
using static {typeof(Kernel).FullName};
");

            kernel.DeferCommand(command);

            return kernel;
        }

        public static CSharpKernel UseNugetDirective(this CSharpKernel kernel)
        {
            var packageRefArg = new Argument<PackageReference>(
                result =>
                {
                    var token = result.Tokens.Select(t => t.Value).SingleOrDefault();
                    if (PackageReference.TryParse(token, out var reference))
                    {
                        return reference;
                    }
                    else
                    {
                        result.ErrorMessage = $"Could not parse \"{token}\" as a package reference.";
                        return null;
                    }
                })
            {
                Name = "package"
            };

            var poundR = new Command("#r")
            {
                packageRefArg
            };

            var restoreContext = new PackageRestoreContext();
            kernel.RegisterForDisposal(restoreContext);

            poundR.Handler = CommandHandler.Create<PackageReference, KernelInvocationContext>(HandleAddPackageReference);

            kernel.AddDirective(poundR);

            var restore = new Command("#!nuget-restore")
            {
                Handler = CommandHandler.Create(DoNugetRestore(kernel, restoreContext))
            };

            kernel.AddDirective(restore);

            return kernel;

            async Task HandleAddPackageReference(PackageReference package, KernelInvocationContext pipelineContext)
            {
                var addPackage = new AddPackage(package)
                {
                    Handler = (command, context) =>
                    {
                        if (restoreContext.ResolvedPackageReferences.SingleOrDefault(r => r.PackageName.Equals(package.PackageName, StringComparison.OrdinalIgnoreCase)) is { }
                                resolvedRef && !string.IsNullOrWhiteSpace(package.PackageVersion) && package.PackageVersion != resolvedRef.PackageVersion)
                        {
                            var errorMessage = $"{GenerateErrorMessage(package, resolvedRef)}";
                            context.Publish(new ErrorProduced(errorMessage));
                        }
                        else
                        {
                            var added = restoreContext.AddPackageReference(package.PackageName, package.PackageVersion, package.RestoreSources);

                            if (!added)
                            {
                                var errorMessage = $"{GenerateErrorMessage(package)}";
                                context.Publish(new ErrorProduced(errorMessage));
                            }
                        }

                        return Task.CompletedTask;
                    }
                };

                await pipelineContext.HandlingKernel.SendAsync(addPackage);
            }

            static string GenerateErrorMessage(
                PackageReference requested,
                ResolvedPackageReference existing = null)
            {
                if (existing != null &&
                    !string.IsNullOrEmpty(requested.PackageName) &&
                    !string.IsNullOrEmpty(requested.PackageVersion))
                {
                    return $"{requested.PackageName} version {requested.PackageVersion} cannot be added because version {existing.PackageVersion} was added previously.";
                }

                return $"Invalid Package specification: '{requested}'";
            }
        }

        private class PackageReferenceComparer : IEqualityComparer<PackageReference>
        {
            public bool Equals(PackageReference x, PackageReference y) =>
                string.Equals(
                    GetDisplayValueId(x),
                    GetDisplayValueId(y),
                    StringComparison.OrdinalIgnoreCase);

            public int GetHashCode(PackageReference obj) => obj.PackageName.ToLowerInvariant().GetHashCode();

            public static string GetDisplayValueId(PackageReference package)
            {
                var value = package.PackageName;

                if (string.IsNullOrWhiteSpace(value))
                {
                    value = package.RestoreSources;
                }

                return value.ToLowerInvariant();
            }
        }

        internal static KernelCommandInvocation DoNugetRestore(
            CSharpKernel kernel, 
            PackageRestoreContext restoreContext)
        {
            return async (command, invocationContext) =>
            {
                KernelCommandInvocation restore = async (_, context) =>
                {
                    var messages = new Dictionary<PackageReference, string>(new PackageReferenceComparer());

                    foreach (var package in restoreContext.RequestedPackageReferences)
                    {
                        if (string.IsNullOrWhiteSpace(package.PackageName) && 
                            string.IsNullOrWhiteSpace(package.RestoreSources))
                        {
                            context.Publish(new ErrorProduced($"Invalid Package Id: '{package.PackageName}'{Environment.NewLine}"));
                        }
                        else
                        {
                            var message =  InstallingPackageMessage(package) + "...";
                            context.Publish(
                                new DisplayedValueProduced(
                                    message, 
                                    context.Command, 
                                    valueId: PackageReferenceComparer.GetDisplayValueId(package)));
                            messages.Add(package, message);
                        }
                    }

                    // Restore packages
                    var restorePackagesTask = restoreContext.Restore();
                    while (await Task.WhenAny(Task.Delay(500), restorePackagesTask) != restorePackagesTask)
                    {
                        foreach (var key in messages.Keys.ToArray())
                        {
                            var message = messages[key] + ".";
                            context.Publish(new DisplayedValueUpdated(message, PackageReferenceComparer.GetDisplayValueId(key)));
                            messages[key] = message;
                        }
                    }

                    var helper = kernel.NativeAssemblyLoadHelper;

                    var result = await restorePackagesTask;

                    if (result.Succeeded)
                    {
                        var nativeLibraryProbingPaths = result.NativeLibraryProbingPaths;
                        helper?.AddNativeLibraryProbingPaths(nativeLibraryProbingPaths);

                        var addedAssemblyPaths =
                            result
                                .ResolvedReferences
                                .SelectMany(added => added.AssemblyPaths)
                                .Distinct()
                                .ToArray();

                        if (helper != null)
                        {
                            foreach (var addedReference in result.ResolvedReferences)
                            {
                                helper.Handle(addedReference);
                            }
                        }

                        kernel.AddScriptReferences(result.ResolvedReferences);

                        foreach (var resolvedReference in result.ResolvedReferences)
                        {
                            context.Publish(
                                new DisplayedValueUpdated(
                                    $"Installed package {resolvedReference.PackageName} version {resolvedReference.PackageVersion}",
                                    PackageReferenceComparer.GetDisplayValueId(resolvedReference)));

                            context.Publish(new PackageAdded(resolvedReference));
                        }
                    }
                    else
                    {
                        var errors = $"{string.Join(Environment.NewLine, result.Errors)}";

                        foreach (var resolvedReference in result.ResolvedReferences)
                        {
                            context.Publish(string.IsNullOrEmpty(resolvedReference.PackageName)
                                ? new ErrorProduced($"Failed to apply RestoreSources {resolvedReference.RestoreSources}{Environment.NewLine}{errors}")
                                : new ErrorProduced($"Failed to add reference to package {resolvedReference.PackageName}{Environment.NewLine}{errors}"));
                        }
                    }
                };

                await invocationContext.QueueAction(restore);
            };

            static string InstallingPackageMessage(PackageReference package)
            {
                string message = null;

                if (!string.IsNullOrEmpty(package.PackageName))
                {
                    message = $"Installing package {package.PackageName}";
                    if (!string.IsNullOrWhiteSpace(package.PackageVersion))
                    {
                        message += $", version {package.PackageVersion}";
                    }

                }
                else if (!string.IsNullOrEmpty(package.RestoreSources))
                {
                    message += $"    RestoreSources: {package.RestoreSources}" + br;
                }

                return message;
            }
        }

        public static CSharpKernel UseWho(this CSharpKernel kernel)
        {
            kernel.AddDirective(who_and_whos());
            Formatter.Register(new CurrentVariablesFormatter());
            return kernel;
        }

        private static Command who_and_whos()
        {
            var command = new Command("%whos")
            {
                Handler = CommandHandler.Create((ParseResult parseResult, KernelInvocationContext context) =>
                {
                    var alias = parseResult.CommandResult.Token.Value;

                    var detailed = alias == "%whos";

                    if (context.Command is SubmitCode &&
                        context.HandlingKernel is CSharpKernel kernel)
                    {
                        var variables = kernel.ScriptState.Variables.Select(v => new CurrentVariable(v.Name, v.Type, v.Value));

                        var currentVariables = new CurrentVariables(
                            variables,
                            detailed);

                        var html = currentVariables
                            .ToDisplayString(HtmlFormatter.MimeType);

                        context.Publish(
                            new DisplayedValueProduced(
                                html,
                                context.Command,
                                new[]
                                {
                                    new FormattedValue(
                                        HtmlFormatter.MimeType,
                                        html)
                                }));
                    }

                    return Task.CompletedTask;
                })
            };

            command.AddAlias("%who");

            return command;
        }
    }
}