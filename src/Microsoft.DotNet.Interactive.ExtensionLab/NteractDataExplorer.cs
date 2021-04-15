// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Http;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class NteractDataExplorer: DataExplorer<TabularDataResource>
    {
        private static Uri _defaultLibraryUri;
        private static string _defaultLibraryVersion;
        private static string _defaultCacheBuster;

        public Uri LibraryUri { get; set; }
        public string LibraryVersion { get; set; }
        public string CacheBuster { get; set; }

        public static void SetDefaultConfiguration(Uri libraryUri, string libraryVersion, string cacheBuster)
        {
            _defaultLibraryUri = libraryUri;
            _defaultLibraryVersion = libraryVersion;
            _defaultCacheBuster = cacheBuster;
        }

        public static void ResetDefaultConfiguration()
        {
            SetDefaultConfiguration(null, null, null);
        }
        public NteractDataExplorer(TabularDataResource data) : base(data)
        {
            LibraryUri = _defaultLibraryUri;
            LibraryVersion = _defaultLibraryVersion;
            CacheBuster = _defaultCacheBuster;
        }

        protected override IHtmlContent ToHtml()
        {
            var requireUri = new Uri("https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js");
            var divId = Id;
            var data = Data.ToJson();
            var code = new StringBuilder();
            var functionName = $"renderNteractDataExplorer_{divId}";

            code.AppendLine("<div style=\"background-color:white;\">");
            code.AppendLine($"<div id=\"{divId}\" style=\"height: 100ch ;margin: 2px;\">");
            code.AppendLine("</div>");
            code.AppendLine(@"<script type=""text/javascript"">");
            AppendWidgetCode(code, data, divId, functionName, LibraryUri, LibraryVersion, CacheBuster);
            code.AppendLine(JavascriptUtilities.GetCodeForEnsureRequireJs(requireUri, functionName));
            code.AppendLine(" </script>");
            code.AppendLine("</div>");

            var html = new HtmlString(code.ToString());
            return html;
        }

        private static void AppendWidgetCode(StringBuilder stringBuilder, TabularDataResourceJsonString data,
            string divId,
            string functionName, Uri libraryUri, string libraryVersion, string cacheBuster)
        {
            libraryVersion ??= "1.0.0";
            stringBuilder.AppendLine($@"
let {functionName} = () => {{");
            if (libraryUri is not null)
            {
                var libraryAbsoluteUri = libraryUri.AbsoluteUri.Replace(".js", string.Empty);
                cacheBuster ??= libraryAbsoluteUri.GetHashCode().ToString("0");
                stringBuilder.AppendLine($@"
    (require.config({{ 'paths': {{ 'context': '{libraryVersion}', 'nteractUri' : '{libraryAbsoluteUri}', 'urlArgs': 'cacheBuster={cacheBuster}' }}}}) || require)(['nteractUri'], (nteract) => {{");
            }
            else
            {
                stringBuilder.AppendLine($@"
    configureRequireFromExtension('nteract','{libraryVersion}')(['nteract/nteractapi'], (nteract) => {{");
            }

            stringBuilder.AppendLine($@"
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