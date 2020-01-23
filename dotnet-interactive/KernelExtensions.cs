// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
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
            var about = new Command("#!about")
            {
                Handler = CommandHandler.Create<KernelInvocationContext>(
                    async context => await context.DisplayAsync(VersionSensor.Version()))
            };

            kernel.AddDirective(about);

            Formatter<VersionSensor.BuildInfo>.Register((info, writer) =>
            {
                // https://github.com/dotnet/swag/tree/master/netlogo
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
    }
}