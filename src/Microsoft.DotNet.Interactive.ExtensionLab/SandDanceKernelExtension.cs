// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Http;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class SandDanceKernelExtension : IKernelExtension, IStaticContentSource
    {
        public string Name => "SandDance";
        public Task OnLoadAsync(Kernel kernel)
        {
            kernel.UseSandDanceExplorer();
            kernel.RegisterForDisposal(() => DataExplorerExtensions.Settings.RestoreDefault());

            KernelInvocationContext.Current?.Display(
                new HtmlString($@"<details><summary>Explore data visually using the <a href=""https://github.com/microsoft/SandDance"">SandDance Explorer</a>.</summary>
    <p>This extension adds the ability to sort, filter, and visualize data using the <a href=""https://github.com/microsoft/SandDance"">SandDance Explorer</a>. Use the <code>ExploreWithSandDance</code> extension method with variables of type <code>IEnumerable<T></code> or <code>IDataView</code> to render the data explorer.</p>
    <img src=""https://user-images.githubusercontent.com/547415/109559345-621e5880-7a8f-11eb-8b98-d4feeaac116f.png"" width=""75%"">
    </details>"),
                "text/html");

            return Task.CompletedTask;
        }
    }

    public class SandDanceExplorer
    {
        internal void LoadData<T>(IEnumerable<T> source)
        {
            LoadData(source.ToTabularJsonString());
        }

        internal void LoadData(TabularJsonString source)
        {
            TabularData = source;
        }

        internal TabularJsonString TabularData { get; set; }
    }

    public static class SandDanceExplorerExtensions
    {
       

        public static DataExplorerSettings Settings { get; } = new();

        public static T UseSandDanceExplorer<T>(this T kernel) where T : Kernel
        {
            RegisterFormatters();
            return kernel;
        }

        public static void RegisterFormatters()
        {
            Formatter.Register<SandDanceExplorer>((explorer, writer) =>
            {
                var html = explorer.TabularData.RenderSandDanceExplorer();
                writer.Write(html);
            }, HtmlFormatter.MimeType);
        }

        public static SandDanceExplorer ExploreWithSandDance<T>(this IEnumerable<T> source)
        {
            var explorer = new SandDanceExplorer();
            explorer.LoadData(source);
            return explorer;
        }

        public static SandDanceExplorer ExploreWithSandDance(this  TabularJsonString source)
        {
            var explorer = new SandDanceExplorer();
            explorer.LoadData(source);
            return explorer;
        }

        private static HtmlString RenderSandDanceExplorer(this TabularJsonString data)
        {
            var divId = Guid.NewGuid().ToString("N");
            var code = new StringBuilder();
            code.AppendLine("<div style=\"background-color:white;\">");
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
            var functionName = $"renderSandDanceExplorer_{divId}";
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
    (require.config({{ 'paths': {{ 'context': '{context}', 'sandDanceUri' : '{absoluteUri}', 'urlArgs': 'cacheBuster={cacheBuster}' }}}}) || require)(['sandDanceUri'], (sandDance) => {{");
            }
            else
            {
                code.AppendLine($@"
    configureRequireFromExtension('SandDance','{context}')(['sandDance/sanddanceapi'], (sandDance) => {{");
            }

            code.AppendLine($@"
        sandDance.createSandDanceExplorer({{
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