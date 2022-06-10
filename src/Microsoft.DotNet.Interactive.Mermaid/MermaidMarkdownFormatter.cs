// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Http;

namespace Microsoft.DotNet.Interactive.Mermaid;

internal class MermaidMarkdownFormatter : ITypeFormatterSource
{
    private static readonly Uri DefaultLibraryUri = new(@"https://cdn.jsdelivr.net/npm/mermaid@9.1.1/dist/mermaid.min.js", UriKind.Absolute);
    private static readonly Uri RequireUri = new("https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js");
    private const string DefaultLibraryVersion = "9.1.1";
    
    private static string? _cacheBuster;
    private static Uri? _libraryUri;
    private static string? _libraryVersion;

    internal static Uri? LibraryUri
    {
        get => _libraryUri ??= DefaultLibraryUri;
        set => _libraryUri = value;
    }

    internal static string? LibraryVersion
    {
        get => _libraryVersion ??= DefaultLibraryVersion; 
        set => _libraryVersion = value;
    }

    internal static string? CacheBuster
    {
        get => _cacheBuster ??= Guid.NewGuid().ToString("N");
        set => _cacheBuster = value;
    }

    public IEnumerable<ITypeFormatter> CreateTypeFormatters()
    {
        yield return new HtmlFormatter<MermaidMarkdown>((value, context) =>
        {
            var html = GenerateHtml(value, LibraryUri!,
                LibraryVersion!);
            html.WriteTo(context.Writer, HtmlEncoder.Default);
        });
    }

    internal static IHtmlContent GenerateHtml(MermaidMarkdown markdown, Uri libraryUri, string libraryVersion)
    {
       
        var divId = Guid.NewGuid().ToString("N");
        var code = new StringBuilder();
        var functionName = $"loadMermaid_{divId}";
        code.AppendLine($"<div class=\"mermaidMarkdownContainer\" style=\"background-color:{markdown.Background}\">");

        code.AppendLine(@"<script type=""text/javascript"">");
        AppendJsCode(code, divId, functionName, libraryUri, libraryVersion, markdown.ToString());
        code.AppendLine(JavascriptUtilities.GetCodeForEnsureRequireJs(RequireUri, functionName));
        code.AppendLine("</script>");
        var style = string.Empty;
        if (!string.IsNullOrWhiteSpace(markdown.Width) || !string.IsNullOrWhiteSpace(markdown.Width))
        {
            style = " style=\"";

            if (!string.IsNullOrWhiteSpace(markdown.Width))
            {
                style += $" width:{markdown.Width}; ";
            }

            if (!string.IsNullOrWhiteSpace(markdown.Height))
            {
                style += $" height:{markdown.Height}; ";
            }

            style += "\" ";
        }
        code.AppendLine($"<div id=\"{divId}\"{style}></div>");
        code.AppendLine("</div>");

        var html = new HtmlString(code.ToString());
        return html;
    }

    private static void AppendJsCode(StringBuilder stringBuilder, string divId, string functionName, Uri libraryUri, string libraryVersion, string markdown)
    {
        stringBuilder.AppendLine($@"
{functionName} = () => {{");

        var libraryAbsoluteUri = Regex.Replace(libraryUri.AbsoluteUri, @"(\.js)$", string.Empty);

        stringBuilder.AppendLine($@" 
        (require.config({{ 'paths': {{ 'context': '{libraryVersion}', 'mermaidUri' : '{libraryAbsoluteUri}', 'urlArgs': 'cacheBuster={CacheBuster}' }}}}) || require)(['mermaidUri'], (mermaid) => {{");


        stringBuilder.AppendLine($@"
            let renderTarget = document.getElementById('{divId}');
            mermaid.mermaidAPI.render( 
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