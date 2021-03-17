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
            kernel.UseNteractDataExplorer();
            
            KernelInvocationContext.Current?.Display(
                new HtmlString($@"<details><summary>Explore data visually using the <a href=""https://github.com/nteract/data-explorer"">nteract Data Explorer</a>.</summary>
    <p>This extension adds the ability to sort, filter, and visualize data using the <a href=""https://github.com/nteract/data-explorer"">nteract Data Explorer</a>. Use the <code>Explore</code> extension method with variables of type <code>IEnumerable<T></code> or <code>IDataView</code> to render the data explorer.</p>
    <img src=""https://user-images.githubusercontent.com/547415/109559345-621e5880-7a8f-11eb-8b98-d4feeaac116f.png"" width=""75%"">
    </details>"),
                "text/html");

            return Task.CompletedTask;
        }

        public string Name => "nteract";
    }

    public static class NteractDataExplorerExtensions
    {
        public static T UseNteractDataExplorer<T>(this T kernel, string uri = null, string context = null, string cacheBuster = null) where T : Kernel
        {
            RegisterFormatters(string.IsNullOrWhiteSpace(uri) ? null : new Uri(uri), context, cacheBuster);
            return kernel;
        }

        private static void RegisterFormatters(Uri uri, string context, string cacheBuster)
        {
            Formatter.Register<NteractDataExplorer>((explorer, writer) =>
            {
                var html = explorer.RenderDataExplorer(uri,context,cacheBuster);
                writer.Write(html);
            }, HtmlFormatter.MimeType);
        }

        private static HtmlString RenderDataExplorer(this NteractDataExplorer explorer, Uri uri, string context, string cacheBuster)
        {
            var divId = explorer.Id;
            var data = explorer.TabularDataResource.ToJson();
            var code = new StringBuilder();
            code.AppendLine("<div style=\"background-color:white;\">");
            code.AppendLine($"<div id=\"{divId}\" style=\"height: 100ch ;margin: 2px;\">");
            code.AppendLine("</div>");
            code.AppendLine(@"<script type=""text/javascript"">");
            GenerateCode(data, code, divId, "https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js", uri, context, cacheBuster);
            code.AppendLine(" </script>");
            code.AppendLine("</div>");
            return new HtmlString(code.ToString());
        }

        private static void GenerateCode(TabularDataResourceJsonString data, StringBuilder code, string divId, string requireUri, Uri uri, string context, string cacheBuster)
        {
            var functionName = $"renderDataExplorer_{divId}";
            GenerateFunctionCode(data, code, divId, functionName, uri, context, cacheBuster);
            GenerateRequireLoader(code, functionName, requireUri);
        }

        private static void GenerateRequireLoader(StringBuilder code, string functionName, string requireUri)
        {
            code.AppendLine(JavascriptUtilities.GetCodeForEnsureRequireJs(new Uri(requireUri), functionName));
        }

        private static void GenerateFunctionCode(TabularDataResourceJsonString data, StringBuilder code, string divId, string functionName, Uri uri, string context, string cacheBuster)
        {
            context ??= "1.0.0";
            code.AppendLine($@"
let {functionName} = () => {{");
            if (uri != null)
            {
                var absoluteUri = uri.AbsoluteUri.Replace(".js", string.Empty);
                cacheBuster ??= absoluteUri.GetHashCode().ToString("0");
                code.AppendLine($@"
    (require.config({{ 'paths': {{ 'context': '{context}', 'nteractUri' : '{absoluteUri}', 'urlArgs': 'cacheBuster={cacheBuster}' }}}}) || require)(['nteractUri'], (nteract) => {{");
            }
            else
            {
                code.AppendLine($@"
    configureRequireFromExtension('nteract','{context}')(['nteract/nteractapi'], (nteract) => {{");
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