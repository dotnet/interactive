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
    private const string DefaultLibraryVersion = "10.1.0";
    private static readonly Uri DefaultLibraryUri = new($@"https://cdn.jsdelivr.net/npm/mermaid@{DefaultLibraryVersion}/dist/mermaid.esm.min.mjs", UriKind.Absolute);
    private static readonly Uri RequireUri = new("https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js");
    
    
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
        code.AppendLine(@"<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.2.0/css/all.min.css"">");
            
       
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

        code.AppendLine(@"<script type=""module"">");
        AppendJsCode(code, divId, libraryUri, libraryVersion, markdown.ToString());
        code.AppendLine("</script>");

        code.AppendLine("</div>");

        var html = new HtmlString(code.ToString());
        return html;
    }

    private static void AppendJsCode(StringBuilder stringBuilder, string divId,  Uri libraryUri, string libraryVersion, string markdown)
    {
        var escapedMarkdown = Regex.Replace(markdown, @"(?<pre>[^\\])(?<newLine>\\n)", @"${pre}\\n");

        stringBuilder.AppendLine($@"
            import mermaid from '{libraryUri.AbsoluteUri}';
            let renderTarget = document.getElementById('{divId}');
            try {{
                const {{svg, bindFunctions}} = await mermaid.mermaidAPI.render( 
                    'mermaid_{divId}', 
                    `{escapedMarkdown}`);
                renderTarget.innerHTML = svg;
                bindFunctions?.(renderTarget);
            }}
            catch (error) {{
                console.log(error);
            }}");
    }
}