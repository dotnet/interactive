// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.DotNet.Interactive.Utility;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.App;

public static class KernelExtensions
{
    public static T UseAboutMagicCommand<T>(this T kernel)
        where T : Kernel
    {

        var about = new Command("#!about", LocalizationResources.Magics_about_Description())
        {
            Handler = CommandHandler.Create((InvocationContext ctx) =>
            {
                ctx.GetService<KernelInvocationContext>().Display(BuildInfo.GetBuildInfo(typeof(Program).Assembly));
                return Task.CompletedTask;
            })
        };

        kernel.AddDirective(about);

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
                            p("© 2020 Microsoft Corporation"),
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
            if (command is SubmitCode submitCode)
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

        Dictionary<string, double> GetStandardMeasurementsFromEvent(KernelEvent event1)
        {
            return new Dictionary<string, double>
            {
                ["ExecutionOrder"] = ++executionOrder,
                ["Succeeded"] = event1 is CommandSucceeded ? 1 : 0
            };
        }
    }
}
