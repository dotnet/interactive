// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Tests;
using XPlot.DotNet.Interactive.KernelExtensions;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public partial class XplotKernelExtensionTests : LanguageKernelTestBase
    {
     
        public XplotKernelExtensionTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task When_a_chart_is_returned_the_value_produced_has_html_with_the_require_config_call()
        {
            var kernel = CreateKernel();
            kernel.UseXplot();

            await kernel.SendAsync(new SubmitCode("using XPlot.Plotly;"));
            await kernel.SendAsync(new SubmitCode("new PlotlyChart()"));

            KernelEvents
                .Should()
                .ContainSingle<ReturnValueProduced>()
                .Which
                .FormattedValues
                .Should()
                .ContainSingle(v => v.MimeType == HtmlFormatter.MimeType)
                .Which
                .Value
                .Should()
                .Contain(
                    "var xplotRequire = require.config({context:'xplot-3.0.1',paths:{plotly:'https://cdn.plot.ly/plotly-1.49.2.min'}}) || require;")
                .And
                .Contain("xplotRequire([\'plotly\'], function(Plotly)")
                .And
                .Contain("<script type=\"text/javascript\">");
        }
    }
}