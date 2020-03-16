// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using System.CommandLine.Rendering;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
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
            var iDirective = new Command("#i", "Specifies an assembly search path or NuGet package source")
            {
                new Argument<string>("source")
            };
            iDirective.Handler = CommandHandler.Create<string, KernelInvocationContext>((source, context) =>
            {
                restoreContext.AddRestoreSource(source.Replace("nuget:", ""));

                IHtmlContent content = div(
                    strong("Restore sources"),
                    ul(restoreContext.RestoreSources
                                    .Select(s => li(span(s)))));

                context.DisplayAsync(content);
            });
            return iDirective;
        }

        private static Command r(PackageRestoreContext restoreContext)
        {
            var rDirective = new Command("#r", "References an assembly or package")
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

                        if (token != null &&
                            !token.StartsWith("nuget:") &&
                            !Path.EndsInDirectorySeparator(token))
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
                if (package?.Value is PackageReference pkg)
                {
                    var alreadyGotten = restoreContext.ResolvedPackageReferences
                                                      .Concat(restoreContext.RequestedPackageReferences)
                                                      .FirstOrDefault(r => r.PackageName.Equals(pkg.PackageName, StringComparison.OrdinalIgnoreCase));

                    if (alreadyGotten is { } && !string.IsNullOrWhiteSpace(pkg.PackageVersion) && pkg.PackageVersion != alreadyGotten.PackageVersion)
                    {
                        var errorMessage = GenerateErrorMessage(pkg, alreadyGotten).ToString(OutputMode.NonAnsi);
                        context.Publish(new ErrorProduced(errorMessage));
                    }
                    else
                    {
                        var added = restoreContext.GetOrAddPackageReference(pkg.PackageName, pkg.PackageVersion);

                        if (added is null)
                        {
                            var errorMessage = GenerateErrorMessage(pkg).ToString(OutputMode.NonAnsi);

                            context.Publish(new ErrorProduced(errorMessage));
                        }
                    }

                    static TextSpan GenerateErrorMessage(
                        PackageReference requested,
                        PackageReference existing = null)
                    {
                        var spanFormatter = new TextSpanFormatter();
                        if (existing != null)
                        {
                            if (!string.IsNullOrEmpty(requested.PackageName))
                            {
                                if (!string.IsNullOrEmpty(requested.PackageVersion))
                                {
                                    return spanFormatter.ParseToSpan(
                                        $"{Ansi.Color.Foreground.Red}{requested.PackageName} version {requested.PackageVersion} cannot be added because version {existing.PackageVersion} was added previously.{Ansi.Color.Off}");
                                }
                            }
                        }

                        return spanFormatter.ParseToSpan($"Invalid Package specification: '{requested}'");
                    }
                }

                return Task.CompletedTask;
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

            public static IEqualityComparer<PackageReference> Instance { get; } = new PackageReferenceComparer();
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
                    var displayedValues = new Dictionary<string, DisplayedValue>();

                    var newlyRequestedPackages =
                        restoreContext.RequestedPackageReferences
                                      .Except(restoreContext.ResolvedPackageReferences, PackageReferenceComparer.Instance);

                    foreach (var package in newlyRequestedPackages)
                    {
                        var message = InstallingPackageMessage(package) + "...";
                        var displayedValue = context.Display(message);
                        displayedValues[PackageReferenceComparer.GetDisplayValueId(package)] = displayedValue;
                        messages.Add(package, message);
                    }

                    // Restore packages
                    var restorePackagesTask = restoreContext.RestoreAsync();
                    while (await Task.WhenAny(Task.Delay(500), restorePackagesTask) != restorePackagesTask)
                    {
                        foreach (var key in messages.Keys.ToArray())
                        {
                            var message = messages[key] + ".";
                            var displayedValue = displayedValues[PackageReferenceComparer.GetDisplayValueId(key)];
                            displayedValue.Update(message);
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
                            if (displayedValues.TryGetValue(
                                PackageReferenceComparer.GetDisplayValueId(resolvedReference), out var displayedValue))
                            {
                                displayedValue.Update(
                                    $"Installed package {resolvedReference.PackageName} version {resolvedReference.PackageVersion}");
                            }

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
            var command = new Command("#!whos", "Display the names of the current top-level variables and their values.")
            {
                Handler = CommandHandler.Create((ParseResult parseResult, KernelInvocationContext context) =>
                {
                    var alias = parseResult.CommandResult.Token.Value;

                    var detailed = alias == "#!whos";

                    Display(context, detailed);

                    return Task.CompletedTask;
                })
            };

            // FIX: (who_and_whos) this should be a separate command with separate help
            command.AddAlias("#!who");

            return command;

            void Display(KernelInvocationContext context, bool detailed)
            {
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
            }
        }


    }
}