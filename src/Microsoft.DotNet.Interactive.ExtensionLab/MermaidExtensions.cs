// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Http;

namespace System {
}

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public static class MermaidExtensions
    {
        public static T UseMermaid<T>(this T kernel, string libraryUri = null, string libraryVersion = null, string cacheBuster = null) where T : Kernel
        {
            Formatter.Register<MermaidMarkdown>((markdown, writer) =>
            {
                var html = GenerateHtml(markdown, string.IsNullOrWhiteSpace(libraryUri) ? null : new Uri(libraryUri),
                    libraryVersion,
                    cacheBuster);
                html.WriteTo(writer, HtmlEncoder.Default);
            }, HtmlFormatter.MimeType);

            return kernel;
        }

        private static IHtmlContent GenerateHtml(MermaidMarkdown markdown, Uri libraryUri, string libraryVersion, string cacheBuster)
        {
            var requireUri = new Uri("https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js");
            var divId = Guid.NewGuid().ToString("N");
            var code = new StringBuilder();
            var functionName = $"loadMermaid_{divId}";
            code.AppendLine("<div style=\"background-color:white;\">");
           
            code.AppendLine(@"<script type=""text/javascript"">");
            code.AppendJsCode(divId, functionName, libraryUri, libraryVersion, cacheBuster, markdown.ToString());
            code.AppendLine(JavascriptUtilities.GetCodeForEnsureRequireJs(requireUri, functionName));
            code.AppendLine("</script>");

            code.AppendLine($"<div id=\"{divId}\"></div>");
            code.AppendLine("</div>");

            var html = new HtmlString(code.ToString());
            return html;
        }

        private static void AppendJsCode(this StringBuilder stringBuilder,
            string divId, string functionName, Uri libraryUri, string libraryVersion, string cacheBuster, string  markdown)
        {
            libraryVersion ??= "1.0.0";
            stringBuilder.AppendLine($@"
let {functionName} = () => {{");
            if (libraryUri != null)
            {
                var libraryAbsoluteUri = libraryUri.AbsoluteUri.Replace(".js", string.Empty);
                cacheBuster ??= libraryAbsoluteUri.GetHashCode().ToString("0");
                stringBuilder.AppendLine($@" 
        (require.config({{ 'paths': {{ 'context': '{libraryVersion}', 'mermaidUri' : '{libraryAbsoluteUri}', 'urlArgs': 'cacheBuster={cacheBuster}' }}}}) || require)(['mermaidUri'], (mermaid) => {{");
            }
            else
            {
                stringBuilder.AppendLine($@"
        configureRequireFromExtension('Mermaid','{libraryVersion}')(['Mermaid/mermaidapi'], (mermaid) => {{");
            }

            stringBuilder.AppendLine($@"
            let renderTarget = document.getElementById('{divId}');
            mermaid.render( 
                'mermaid_{divId}', 
                `{markdown}`, 
                g => {{
                    renderTarget.innerHTML = g 
                }});
        }},
        (error) => {{
            console.log(error);
        }});
}}");
        }
    }
}