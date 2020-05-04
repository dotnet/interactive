// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using HtmlAgilityPack;
using XPlot.DotNet.Interactive.KernelExtensions;
using XPlot.Plotly;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public partial class XplotKernelExtensionTests
    {
        public class GetChartHtmlTests
        {
            [Fact]
            public void Returns_the_html_with_div()
            {
                var chart = new PlotlyChart();
                var html = PlotlyChartExtensions.GetHtml(chart);
                var document = new HtmlDocument();
                document.LoadHtml(html);

                document.DocumentNode.SelectSingleNode("//div").InnerHtml.Should().NotBeNull();
                document.DocumentNode.SelectSingleNode("//div").Id.Should().NotBeNullOrEmpty();
            }

            [Fact]
            public void Returns_the_html_with_script_containing_require_config()
            {
                var chart = new PlotlyChart();
                var html = PlotlyChartExtensions.GetHtml(chart);
                var document = new HtmlDocument();
                document.LoadHtml(html);

                document.DocumentNode.SelectSingleNode("//script")
                    .InnerHtml
                    .Should()
                    .Contain("var xplotRequire = requirejs.config({context:'xplot-3.0.1',paths:{plotly:'https://cdn.plot.ly/plotly-1.49.2.min'}});");
            }

            [Fact]
            public void Returns_the_html_with_script_containing_require_plotly_and_call_to_new_plot_function()
            {
                var chart = new PlotlyChart();
                var html = PlotlyChartExtensions.GetHtml(chart);
                var document = new HtmlDocument();
                document.LoadHtml(html);

                var divId = document.DocumentNode.SelectSingleNode("//div").Id;
                document.DocumentNode
                    .SelectSingleNode("//script")
                    .InnerHtml.Split("\n")
                    .Select(item => item.Trim())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Should()
                    .ContainInOrder(@"xplotRequire(['plotly'], function(Plotly) {",
                                        "var data = null;",
                                         @"var layout = """";",
                                         $"Plotly.newPlot('{divId}', data, layout);");
            }
        }

    }
}