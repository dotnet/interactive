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
            var restoreContext = new PackageRestoreContext();
            kernel.SetProperty(restoreContext);
            kernel.RegisterForDisposal(restoreContext);

            kernel.AddDirective(i(restoreContext));
            kernel.AddDirective(r(restoreContext));

            var restore = new Command("#!nuget-restore")
            {
                Handler = CommandHandler.Create(DoNugetRestore(kernel, restoreContext)),
                IsHidden = true
            };

            kernel.AddDirective(restore);

            return kernel;
        }

        private static Command i(PackageRestoreContext restoreContext)
        {
            var iDirective = new Command("#i")
            {
                new Argument<string>("source")
            };
            iDirective.Handler = CommandHandler.Create<string, KernelInvocationContext>((source, context) =>
            {
                restoreContext.AddRestoreSource(source.Replace("nuget:", ""));
            });
            return iDirective;
        }

        private static Command r(PackageRestoreContext restoreContext)
        {
            var rDirective = new Command("#r")
            {
                new Argument<PackageReference>(
                    result =>
                    {
                        var token = result.Tokens.Select(t => t.Value).SingleOrDefault();
                        if (PackageReference.TryParse(token, out var reference))
                        {
                            return reference;
                        }
                        else
                        {
                            result.ErrorMessage = $"Unable to parse package reference: \"{token}\"";
                            return null;
                        }
                    })
                {
                    Name = "package"
                }
            };

            rDirective.Handler = CommandHandler.Create<PackageReference, KernelInvocationContext>(HandleAddPackageReference);

            return rDirective;

            async Task HandleAddPackageReference(
                PackageReference package, 
                KernelInvocationContext pipelineContext)
            {
                var addPackage = new AddPackage(package)
                {
                    Handler = (command, context) =>
                    {
                        var alreadyGotten =
                            restoreContext.ResolvedPackageReferences
                                          .Concat(restoreContext.RequestedPackageReferences)
                                          .FirstOrDefault(r => r.PackageName.Equals(package.PackageName, StringComparison.OrdinalIgnoreCase));

                        if (alreadyGotten is { } &&
                            !string.IsNullOrWhiteSpace(package.PackageVersion) &&
                            package.PackageVersion != alreadyGotten.PackageVersion)
                        {
                            var errorMessage = $"{GenerateErrorMessage(package, alreadyGotten)}";
                            context.Publish(new ErrorProduced(errorMessage));
                        }
                        else
                        {
                            var added = restoreContext.GetOrAddPackageReference(
                                package.PackageName,
                                package.PackageVersion);

                            if (added is null)
                            {
                                var errorMessage = $"{GenerateErrorMessage(package)}";
                                context.Publish(new ErrorProduced(errorMessage));
                            }
                        }

                        return Task.CompletedTask;
                    }
                };

                await pipelineContext.HandlingKernel.SendAsync(addPackage);

                static string GenerateErrorMessage(
                    PackageReference requested,
                    PackageReference existing = null)
                {
                    if (existing != null)
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
                return package.PackageName.ToLowerInvariant();
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

                        var message = InstallingPackageMessage(package) + "...";
                        context.Publish(
                            new DisplayedValueProduced(
                                message,
                                context.Command,
                                valueId: PackageReferenceComparer.GetDisplayValueId(package)));
                        messages.Add(package, message);
                    }

                    // Restore packages
                    var restorePackagesTask = restoreContext.RestoreAsync();
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

                       context.Fail(message: errors);
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
            var command = new Command("#!whos")
            {
                Handler = CommandHandler.Create((ParseResult parseResult, KernelInvocationContext context) =>
                {
                    var alias = parseResult.CommandResult.Token.Value;

                    var detailed = alias == "#!whos";

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

            command.AddAlias("#!who");

            return command;
        }
    }
}