// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class NteractKernelExtension : IKernelExtension, IStaticContentSource
    {
        public Task OnLoadAsync(Kernel kernel)
        {
            kernel.UseDataExplorer();

            KernelInvocationContext.Current?.Display(
                $@"Added the `Explore` extension method, which you can use with `IEnumerable<T>` and `IDataView` to view data using the [nteract Data Explorer](https://github.com/nteract/data-explorer).",
                "text/markdown");

            return Task.CompletedTask;
        }

        public string Name => "nteract";
    }

    public static class DataExplorerExtensions
    {
        public static T UseDataExplorer<T>(this T kernel) where T : Kernel
        {
            RegisterFormatters();
            return kernel;
        }

        public static void RegisterFormatters()
        {
            Formatter.Register<TabularJsonString>((explorer, writer) =>
            {
                var html = explorer.RenderDataExplorer();
                writer.Write(html);
            }, HtmlFormatter.MimeType);
        }

        private static HtmlString RenderDataExplorer(this TabularJsonString data)
        {
            var divId = Guid.NewGuid().ToString("N");
            var code = new StringBuilder();
            code.AppendLine("<div>");
            code.AppendLine($"<div id=\"{divId}\" style=\"height: 100ch ;margin: 2px;\">");
            code.AppendLine("</div>");
            code.AppendLine(@"<script type=""text/javascript"">
getExtensionRequire('nteract','1.0.0')(['nteract/index'], (nteract) => {");
            code.AppendLine($@" nteract.createDataExplorer({{
        data: {data},
        container: document.getElementById(""{divId}"")
    }});
}},
(error) => {{ 
    console.log(error); 
}});");
            code.AppendLine(" </script>");
            code.AppendLine("</div>");
            return new HtmlString(code.ToString());
        }
    }
}