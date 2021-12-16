// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Utility;
using Recipes;

using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.App
{
    public static class KernelExtensions
    {
        public static T UseAboutMagicCommand<T>(this T kernel)
            where T : Kernel
        {
            var about = new Command("#!about", "Show version and build information")
            {
                Handler = CommandHandler.Create((InvocationContext ctx) =>
                {
                    ctx.GetService<KernelInvocationContext>().Display(VersionSensor.Version());
                    return Task.CompletedTask;
                })
            };

            kernel.AddDirective(about);

            Formatter.Register<VersionSensor.BuildInfo>((info, writer) =>
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

                PocketView html = table(
                    tbody(
                        tr(
                            td(img[src: encodedImage, width: "125em"]),
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
    }
}
