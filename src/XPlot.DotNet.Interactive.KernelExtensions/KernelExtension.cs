// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Http;
using XPlot.Plotly;

using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace XPlot.DotNet.Interactive.KernelExtensions
{
    public class KernelExtension : IKernelExtension
    {
        public Task OnLoadAsync(Kernel kernel)
        {
            RegisterPlotlyFormatters();

            return Task.CompletedTask;
        }

        public static void RegisterPlotlyFormatters()
        {
            Formatter.Register<PlotlyChart>(
                (chart, writer) => writer.Write(GetHtml(chart)),
                HtmlFormatter.MimeType);
        }

        private static IHtmlContent GetHtml(PlotlyChart chart)
        {
            var divElement = div[style: $"width: {chart.Width}px; height: {chart.Height}px;", id: chart.Id]();
            var jsElement = chart.GetInlineJS().Replace("<script>", string.Empty).Replace("</script>", string.Empty);

            return new HtmlString($@"{divElement}
{GetScriptElementWithRequire(jsElement)}");
        }

        private static IHtmlContent GetScriptElementWithRequire(string script)
        {
            var newScript = new StringBuilder();
            newScript.AppendLine("<script type=\"text/javascript\">");

            newScript.AppendLine(@"
var renderPlotly = function() {
    var xplotRequire = require.config({context:'xplot-3.0.1',paths:{plotly:'https://cdn.plot.ly/plotly-1.49.2.min'}}) || require;
    xplotRequire(['plotly'], function(Plotly) {");

            newScript.AppendLine(script);
            newScript.AppendLine(@"});
};");

            newScript.AppendLine(JavascriptUtilities.GetEnsureRequireJs(new Uri("https://cdnjs.cloudflare.com/ajax/libs/require.js/2.3.6/require.min.js"), "renderPlotly"));
            newScript.AppendLine("</script>");
            return new HtmlString(newScript.ToString());
        }
    }
}