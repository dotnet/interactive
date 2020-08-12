// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.App.Commands;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Server;
using Recipes;
using XPlot.DotNet.Interactive.KernelExtensions;
using XPlot.Plotly;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.App
{
    public static class KernelExtensions
    {
        public static T UseQuitCommand<T>(this T kernel, IDisposable disposeOnQuit, CancellationToken cancellationToken) where T : Kernel
        {
            Quit.DisposeOnQuit = disposeOnQuit;
            KernelCommandEnvelope.RegisterCommandType<Quit>(nameof(Quit));
            cancellationToken.Register(async () =>
            {
                await kernel.SendAsync(new Quit());
            });
            return kernel;
        }

        public static T UseAbout<T>(this T kernel)
            where T : Kernel
        {
            var about = new Command("#!about", "Show version and build information")
            {
                Handler = CommandHandler.Create<KernelInvocationContext>(
                    async context => await context.DisplayAsync(VersionSensor.Version()))
            };

            kernel.AddDirective(about);

            Formatter.Register<VersionSensor.BuildInfo>((info, writer) =>
            {
                var url = "https://github.com/dotnet/interactive";
                var encodedImage = string.Empty;

                var assembly = typeof(Program).Assembly;
                using (var resourceStream = assembly.GetManifestResourceStream($"{typeof(Program).Namespace}.resources.logo-456x456.png"))
                {
                    if (resourceStream != null)
                    {
                        var png = new byte[resourceStream.Length];
                        resourceStream.Read(png, 0, png.Length);
                        encodedImage = $"data:image/png;base64, {Convert.ToBase64String(png)}";
                    }

                }

                PocketView html = table(
                    tbody(
                        tr(
                            td(img[src: encodedImage, width:"125em"]),
                            td[style: "line-height:.8em"](
                                p[style: "font-size:1.5em"](b(".NET Interactive")),
                                p("© 2020 Microsoft Corporation"),
                                p(b("Version: "), info.AssemblyInformationalVersion),
                                p(b("Build date: "), info.BuildDate),
                                p(a[href: url](url))
                            ))
                    ));

                writer.Write(html);
            }, HtmlFormatter.MimeType);

            return kernel;
        }

        public static T UseXplot<T>(this T kernel)
            where T : Kernel
        {
            Formatter.Register<PlotlyChart>(
                (chart, writer) => writer.Write(PlotlyChartExtensions.GetHtml(chart)),
                HtmlFormatter.MimeType);

            return kernel;
        }

        public static T UseHttpApi<T>(this T kernel, StartupOptions startupOptions, HttpProbingSettings httpProbingSettings)
            where T : Kernel
        {

            var initApiCommand = new Command("#!enable-http")
            {
                IsHidden = true,
                Handler = CommandHandler.Create((KernelInvocationContext context) =>
                {
                    if (context.Command is SubmitCode submitCode)
                    {
                        var probingUrls = httpProbingSettings != null
                            ? httpProbingSettings.AddressList
                            : new[]
                            {
                                new Uri($"http://localhost:{startupOptions.HttpPort}")
                            };
                        var html =
                            HttpApiBootstrapper.GetHtmlInjection(probingUrls, startupOptions.HttpPort?.ToString() ?? Guid.NewGuid().ToString("N"));
                        context.Display(html, "text/html");
                        context.Complete(submitCode);
                    }
                })
            };

            kernel.AddDirective(initApiCommand);

            return kernel;
        }
    }
}
