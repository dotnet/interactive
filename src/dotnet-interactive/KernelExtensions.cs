// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PackageManagement;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Telemetry;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.App;

public static class KernelExtensions
{
    public static CSharpKernel UseNugetDirective(this CSharpKernel kernel, bool forceRestore = false)
    {
        kernel.UseNugetDirective((k, resolvedPackageReference) =>
        {
            
            k.AddAssemblyReferences(resolvedPackageReference
                .SelectMany(r => r.AssemblyPaths));
            return Task.CompletedTask;
        }, forceRestore);

        return kernel;
    }

    public static FSharpKernel UseNugetDirective(this FSharpKernel kernel, bool forceRestore = false)
    {
        kernel.UseNugetDirective((k, resolvedPackageReference) =>
        {
            var resolvedAssemblies = resolvedPackageReference
                .SelectMany(r => r.AssemblyPaths);

            var packageRoots = resolvedPackageReference
                .Select(r => r.PackageRoot);

            k.AddAssemblyReferencesAndPackageRoots(resolvedAssemblies, packageRoots);

            return Task.CompletedTask;
        }, forceRestore);

        return kernel;
    }

    public static T UseAboutMagicCommand<T>(this T kernel)
        where T : Kernel
    {
        var aboutDirective = new KernelActionDirective("#!about")
        {
            Description = LocalizationResources.Magics_about_Description()
        };

        kernel.AddDirective(
            aboutDirective,
            (_, context) =>
            {
                context.Display(BuildInfo.GetBuildInfo(typeof(Program).Assembly));
                return Task.CompletedTask;
            }
        );

        Formatter.Register<BuildInfo>((info, writer) =>
        {
            var url = "https://github.com/dotnet/interactive";
            var encodedImage = string.Empty;

            var assembly = typeof(KernelExtensions).Assembly;
            using (var resourceStream = assembly.GetManifestResourceStream($"{typeof(KernelExtensions).Namespace}.resources.logo-456x456.png"))
            {
                if (resourceStream is not null)
                {
                    var png = new byte[resourceStream.Length];
                    resourceStream.Read(png, 0, png.Length);
                    encodedImage = $"data:image/png;base64, {Convert.ToBase64String(png)}";
                }
            }

            var libraryAssembly = typeof(Kernel).Assembly;
            var libraryInformationalVersion = libraryAssembly
                                              .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                              .InformationalVersion;

            PocketView html = table(
                tbody(
                    tr(
                        td(img[src: encodedImage, width: "125em"]),
                        td[style: "line-height:.8em"](
                            p[style: "font-size:1.5em"](b(".NET Interactive")),
                            p("© 2020-2024 Microsoft Corporation"),
                            p(b("Version: "), info.AssemblyInformationalVersion),
                            p(b("Library version: "), libraryInformationalVersion),
                            p(b("Build date: "), info.BuildDate),
                            p(a[href: url](url))
                        ))
                ));

            writer.Write(html);
        }, HtmlFormatter.MimeType);

        return kernel;
    }

    public static CompositeKernel UseSecretManager(this CompositeKernel kernel)
    {
        PowerShellKernel powerShellKernel = null;
        SecretManager secretManager = null;

        kernel.AddMiddleware(async (command, context, next) =>
        {
            if (command is not RequestInput { SaveAs: { } saveAs } requestInput)
            {
                await next(command, context);
                return;
            }

            if (secretManager is null)
            {
                powerShellKernel = kernel.ChildKernels.OfType<PowerShellKernel>().SingleOrDefault();

                if (powerShellKernel is not null)
                {
                    secretManager = new(powerShellKernel);
                }
                else
                {
                    // FIX: (UseSecretManager) what's the best thing to do here? maybe silently ignore? display a warning?
                    await next(command, context);
                    return;
                }
            }

            if (secretManager.TryGetSecret(requestInput.SaveAs, out var value))
            {
                context.Publish(new InputProduced(value, requestInput));

                var message =
                    $"""
                     Using saved value '{requestInput.SaveAs}'. To remove this value, run the following command in a PowerShell cell:
                     
                     ```powershell
                         Remove-Secret -Name "{requestInput.SaveAs}" -Vault {secretManager.VaultName}
                     ```
                     """;
                context.Publish(new DisplayedValueProduced(
                                    message,
                                    requestInput,
                                    [new FormattedValue("text/markdown", message)]));
            }
            else
            {
                using var _ = context.KernelEvents.Subscribe(@event =>
                {
                    if (@event is InputProduced inputProduced && inputProduced.Command.GetOrCreateToken() == requestInput.GetOrCreateToken())
                    {
                        secretManager.SetSecret(requestInput.SaveAs, inputProduced.Value);

                        var message =
                            $"""
                             Saving your response for value '{saveAs}'. To remove this value, run the following command in a PowerShell cell:

                             ```powershell
                                 Remove-Secret -Name "{requestInput.SaveAs}" -Vault {secretManager.VaultName}
                             ```
                             """;
                        context.Publish(new DisplayedValueProduced(
                                            message,
                                            requestInput,
                                            [new FormattedValue("text/markdown", message)]));
                    }
                });

                await next(command, context);
            }
        });

        return kernel;
    }

    public static CompositeKernel UseTelemetrySender(
        this CompositeKernel kernel,
        TelemetrySender telemetrySender)
    {
        var executionOrder = 0;
        var sessionId = Guid.NewGuid().ToString();
        var subscription = kernel.KernelEvents.Subscribe(SendTelemetryFor);

        kernel.AddMiddleware(async (command, context, next) =>
        {
            await next(command, context);

            if (command is SubmitCode)
            {
                var properties = GetStandardPropertiesFromCommand(command);

                telemetrySender.TrackEvent(
                    "CodeSubmitted",
                    properties: properties);
            }
        });

        kernel.RegisterForDisposal(subscription);

        return kernel;

        void SendTelemetryFor(KernelEvent @event)
        {
            switch (@event.Command, kernelEvent: @event)
            {
                case (_, PackageAdded added):
                    {
                        var properties = GetStandardPropertiesFromEvent(@event);
                        properties.Add("PackageName", added.PackageReference.PackageName.ToSha256HashWithNormalizedCasing());
                        properties.Add("PackageVersion", added.PackageReference.PackageVersion.ToSha256Hash());

                        var measurements = GetStandardMeasurementsFromEvent(@event);

                        telemetrySender.TrackEvent(
                            "PackageAdded",
                            measurements: measurements,
                            properties: properties);
                    }
                    break;
            }

        }
        
        Dictionary<string, string> GetStandardPropertiesFromEvent(KernelEvent kernelEvent)
        {
            return GetStandardPropertiesFromCommand(kernelEvent.Command);
        }

        Dictionary<string, string> GetStandardPropertiesFromCommand(KernelCommand kernelCommand)
        {
            Kernel handlingKernel = null;
            if (kernelCommand.TargetKernelName is not null)
            {
                kernel.ChildKernels.TryGetByAlias(kernelCommand.TargetKernelName, out handlingKernel);
            }

            var properties = new Dictionary<string, string>
            {
                ["KernelName"] = kernelCommand.TargetKernelName?.ToSha256Hash(),
                ["KernelLanguageName"] = handlingKernel?.KernelInfo?.LanguageName?.ToSha256Hash(),
                ["KernelSessionId"] = sessionId
            };
            return properties;
        }

        Dictionary<string, double> GetStandardMeasurementsFromEvent(KernelEvent @event)
        {
            return new Dictionary<string, double>
            {
                ["ExecutionOrder"] = ++executionOrder,
                ["Succeeded"] = @event is CommandSucceeded ? 1 : 0
            };
        }
    }
}
