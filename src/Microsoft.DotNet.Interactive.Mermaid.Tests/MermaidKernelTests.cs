// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Assent;
using FluentAssertions;
using FluentAssertions.Execution;
using HtmlAgilityPack;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Mermaid.Tests;

public class MermaidKernelTests 
{
    private readonly Configuration _configuration;
    

    public MermaidKernelTests()
    {

        _configuration = new Configuration()
            .UsingExtension("txt")
            .SetInteractive(Debugger.IsAttached);
    }
    
    [Fact]
    public void registers_html_formatter_for_MermaidMarkdown()
    {
        var markdown = @"graph TD
    A[Client] --> B[Load Balancer]
    B --> C[Server1]
    B --> D[Server2]";

        var formatted = new MermaidMarkdown(markdown).ToDisplayString(HtmlFormatter.MimeType);
        var doc = new HtmlDocument();
        doc.LoadHtml(formatted.FixedGuid().FixedCacheBuster());
        var scriptNode = doc.DocumentNode.SelectSingleNode("//div/script");
        var renderTarget = doc.DocumentNode.SelectSingleNode("//div[@id='00000000000000000000000000000000']");
        using var _ = new AssertionScope();

        scriptNode.Should().NotBeNull();
        scriptNode.InnerText.Should()
            .Contain(markdown);
        scriptNode.InnerText.Should()
            .Contain("import mermaid from ");

        renderTarget.Should().NotBeNull();
    }

    [Fact]
    public async Task mermaid_kernel_handles_SubmitCode()
    {
        using var kernel = new CompositeKernel
        {
            new MermaidKernel()
        };

        var result = await kernel.SubmitCodeAsync(@"graph TD
    A[Client] --> B[Load Balancer]
    B --> C[Server1]
    B --> D[Server2]
");

        var returnValue = result.Events
                                .Should()
                                .ContainSingle<DisplayedValueProduced>()
                                .Which
                                .Value as MermaidMarkdown;

        returnValue.ToString().Should().Be(@"graph TD
    A[Client] --> B[Load Balancer]
    B --> C[Server1]
    B --> D[Server2]
");
        
    }

    [Fact]
    public async Task mermaid_kernel_handles_escaped_strings()
    {
        using var kernel = new CompositeKernel
        {
            new MermaidKernel()
        };

        var result = await kernel.SubmitCodeAsync(@"flowchart LR
    data[(SQL\nQuery Titanic data)] 
    data --data.frame --> R[R fa:fa-gear \nData cleanup \nand Feature engineering\\n w/ ggplot2, dplyr, tidyverse]
    R -- ""pandas.DataFrame"" --> Py[py fa:fa-gear \nPredictive Modeling\n w/ pandas, scikit-learn]
");

        var formattedValue = result.Events
                                   .OfType<DisplayedValueProduced>()
                                   .Single()
                                   .FormattedValues.First(fm => fm.MimeType == HtmlFormatter.MimeType);

        formattedValue.Value.Should().Contain(@"flowchart LR
    data[(SQL\\nQuery Titanic data)] 
    data --data.frame --> R[R fa:fa-gear \\nData cleanup \\nand Feature engineering\\n w/ ggplot2, dplyr, tidyverse]
    R -- ""pandas.DataFrame"" --> Py[py fa:fa-gear \\nPredictive Modeling\\n w/ pandas, scikit-learn]");

    }

    [Fact]
    public async Task can_specify_background_color()
    {
        using var kernel = new CompositeKernel
        {
            new MermaidKernel()
        };

        KernelCommandResult result = await kernel.SubmitCodeAsync(@"
#!mermaid --display-background-color red
    graph TD
    A[Client] --> B[Load Balancer]
    B --> C[Server1]
    B --> D[Server2]
");

        var formattedData = result.Events
                                  .Should()
                                  .ContainSingle<DisplayedValueProduced>()
                                  .Which
                                  .FormattedValues
                                  .Single(fm => fm.MimeType == HtmlFormatter.MimeType)
                                  .Value;

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(formattedData.FixedGuid());

        var style = htmlDoc.DocumentNode
                .SelectSingleNode("//div[contains(concat(' ',normalize-space(@class),' '),' mermaidMarkdownContainer ')]")
                .Attributes["style"].Value;

        style.Should().Be("background-color:red");
    }

    [Fact]
    public async Task can_specify_display_dimensions()
    {
        using var kernel = new CompositeKernel
        {
            new MermaidKernel()
        };

        KernelCommandResult result = await kernel.SubmitCodeAsync(@"
#!mermaid --display-width 200px --display-height 250px
    graph TD
    A[Client] --> B[Load Balancer]
    B --> C[Server1]
    B --> D[Server2]
");

        var formattedData = result.Events
                                  .Should()
                                  .ContainSingle<DisplayedValueProduced>()
                                  .Which
                                  .FormattedValues
                                  .Single(fm => fm.MimeType == HtmlFormatter.MimeType)
                                  .Value;

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(formattedData.FixedGuid());

        var style = htmlDoc.DocumentNode
            .SelectSingleNode("//div[@id='00000000000000000000000000000000']")
            .Attributes["style"].Value;

        style.Should().Be(" width:200px;  height:250px; ");

    }
}