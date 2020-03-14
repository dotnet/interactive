// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Recipes;
using XPlot.DotNet.Interactive.KernelExtensions;
using XPlot.Plotly;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.App
{
    public static class KernelExtensions
    {
        public static T UseAbout<T>(this T kernel)
            where T : KernelBase
        {
            var about = new Command("#!about", "Show version and build information")
            {
                Handler = CommandHandler.Create<KernelInvocationContext>(
                    async context => await context.DisplayAsync(VersionSensor.Version()))
            };

            kernel.AddDirective(about);

            Formatter<VersionSensor.BuildInfo>.Register((info, writer) =>
            {
                var url = "https://github.com/dotnet/interactive";

                PocketView html = table(
                    tbody(
                        tr(
                            td(
                                img[
                                    src: "https://raw.githubusercontent.com/dotnet/swag/master/netlogo/small-200x198/pngs/msft.net_small_purple.png",
                                    width: "125em"]),
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
            where T : KernelBase
        {
            Formatter<PlotlyChart>.Register(
                (chart, writer) => writer.Write(PlotlyChartExtensions.GetHtml(chart)),
                HtmlFormatter.MimeType);

            return kernel;
        }

        public static T UseHttpApi<T>(this T kernel, StartupOptions startupOptions, HttpProbingSettings httpProbingSettings)
            where T : KernelBase
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
                            {new Uri($"http://localhost:{startupOptions.HttpPort}")};
                        
                        var scriptContent =
                            HttpApiBootstrapper.GetJSCode(probingUrls, startupOptions.HttpPort?.ToString() ?? Guid.NewGuid().ToString("N") );

                        string value =
                            script[type: "text/javascript"](

                                    scriptContent.ToHtmlContent())
                                .ToString();

                        context.Publish(new DisplayedValueProduced(
                            scriptContent,
                            context.Command,
                            formattedValues: new[]
                            {
                                new FormattedValue("text/html",
                                    value)
                            }));

                        context.Complete(submitCode);
                    }
                })
            };

            kernel.AddDirective(initApiCommand);

            return kernel;
        }
    }
}