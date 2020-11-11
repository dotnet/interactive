// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Http;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class NteractKernelExtension : IKernelExtension, IStaticContentSource
    {
        public Task OnLoadAsync(Kernel kernel)
        {
            kernel.UseDataExplorer();
            kernel.RegisterForDisposal(() => DataExplorerExtensions.Settings.RestoreDefault());

            KernelInvocationContext.Current?.Display(
                $@"* Adds the `Explore` extension method, which you can use with `IEnumerable<T>` and `IDataView` to view data using the [nteract Data Explorer](https://github.com/nteract/data-explorer).",
                "text/markdown");

            return Task.CompletedTask;
        }

        public string Name => "nteract";
    }

    public static class DataExplorerExtensions
    {
        public static DataExplorerSettings Settings { get; } = new DataExplorerSettings();

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
            code.AppendLine(@"<script type=""text/javascript"">");
            GenerateCode(data, code, divId, "https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js");
            code.AppendLine(" </script>");
            code.AppendLine("</div>");
            return new HtmlString(code.ToString());
        }

        private static void GenerateCode(TabularJsonString data, StringBuilder code, string divId, string requireUri)
        {
            var functionName = $"renderDataExplorer_{divId}";
            GenerateFunctionCode(data, code, divId, functionName);
            GenerateRequireLoader(code, functionName, requireUri);
        }

        private static void GenerateRequireLoader(StringBuilder code, string functionName, string requireUri)
        {
            code.AppendLine(JavascriptUtilities.GetCodeForEnsureRequireJs(new Uri(requireUri), functionName));
        }


        private static void GenerateFunctionCode(TabularJsonString data, StringBuilder code, string divId, string functionName)
        {
            var context = Settings.Context ?? "1.0.0";
            code.AppendLine($@"
let {functionName} = () => {{");
            if (Settings.Uri != null)
            {
                var absoluteUri = Settings.Uri.AbsoluteUri.Replace(".js", string.Empty);
                var cacheBuster = Settings.CacheBuster ?? absoluteUri.GetHashCode().ToString("0");
                code.AppendLine($@"
    (require.config({{ 'paths': {{ 'context': '{context}', 'nteractUri' : '{absoluteUri}', 'urlArgs': 'cacheBuster={cacheBuster}' }}}}) || require)(['nteractUri'], (nteract) => {{");
            }
            else
            {
                code.AppendLine($@"
    configureRequireFromExtension('nteract','{context}')(['nteract/index'], (nteract) => {{");
            }

            code.AppendLine($@"
        nteract.createDataExplorer({{
            data: {data},
            container: document.getElementById(""{divId}"")
        }});
    }},
    (error) => {{
        console.log(error);
    }});
}};");
        }
    }
}