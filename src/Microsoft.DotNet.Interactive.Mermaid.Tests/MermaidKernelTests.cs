// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Assent;
using FluentAssertions;
using FluentAssertions.Execution;
using HtmlAgilityPack;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
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
            .Contain("(['mermaidUri'], (mermaid) => {");

        renderTarget.Should().NotBeNull();
    }

    [Fact]
    public void registers_html_formatter_for_UmlClassDiagram()
    {
        var explorer = typeof(List<string>).ToUmlClassDiagram();
        var formatted = explorer.ToDisplayString(HtmlFormatter.MimeType);
        var markdown = explorer.ToMarkdown().ToString();

        var doc = new HtmlDocument();
        doc.LoadHtml(formatted.FixedGuid().FixedCacheBuster());
        var scriptNode = doc.DocumentNode.SelectSingleNode("//div/script");
        var renderTarget = doc.DocumentNode.SelectSingleNode("//div[@id='00000000000000000000000000000000']");

        using var _ = new AssertionScope();

        scriptNode.Should().NotBeNull();
        scriptNode.InnerText.Should()
            .Contain(markdown);
        scriptNode.InnerText.Should()
            .Contain("(['mermaidUri'], (mermaid) => {");

        renderTarget.Should().NotBeNull();
    }


    [Fact]
    public async Task mermaid_kernel_handles_SubmitCode()
    {
        using var kernel = new CompositeKernel
        {
            new MermaidKernel()
        };

        var result = await kernel.SubmitCodeAsync(@"
#!mermaid
    graph TD
    A[Client] --> B[Load Balancer]
    B --> C[Server1]
    B --> D[Server2]
");

        var events = result.KernelEvents.ToSubscribedList();

        var returnValue = events
            .OfType<DisplayedValueProduced>()
            .Single()
            .Value as MermaidMarkdown;

        returnValue.ToString().Should().Be(@"graph TD
    A[Client] --> B[Load Balancer]
    B --> C[Server1]
    B --> D[Server2]
");
        
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

        var events = result.KernelEvents.ToSubscribedList();

        var formattedData = events
            .OfType<DisplayedValueProduced>()
            .Single()
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

        var events = result.KernelEvents.ToSubscribedList();

        var formattedData = events
            .OfType<DisplayedValueProduced>()
            .Single()
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

    [Fact]
    public async Task can_use_extension_methods_from_the_kernel_extension()
    {
        var kernel = new CompositeKernel
        {
            new CSharpKernel(),
            new MermaidKernel()
        };

        await kernel.SendAsync(new SubmitCode($@"#r ""{typeof(MermaidKernel).Assembly.Location}""", "csharp"));

        var result = await kernel.SendAsync(new SubmitCode(@"
using System;

typeof(List<string>).ToUmlClassDiagram().Display();
", "csharp"));

        var events = result.KernelEvents.ToSubscribedList();

        events.Should().NotContainErrors();
    }

    [Fact]
    public void can_generate_class_diagram_from_type()
    {
        var diagram = typeof(JsonElement).ToUmlClassDiagram().ToMarkdown();

        this.Assent(diagram.ToString());
    }

    [Fact]
    public void can_generate_class_diagram_from_generic_type()
    {
        var diagram = typeof(List<Dictionary<string, object>>).ToUmlClassDiagram().ToMarkdown();

        this.Assent(diagram.ToString());
    }


    [Fact]
    public void can_generate_class_diagram_to_a_specified_depth()
    {
        var diagram = typeof(List<Dictionary<string, object>>).ToUmlClassDiagram(new UmlClassDiagramConfiguration(1));

        diagram.ToString().Should()
            .NotContain(
                "ICollection~Dictionary<String, Object>~ --|> IEnumerable~Dictionary<String, Object>~ : Inheritance");
    }

    [Fact]
    public void can_generate_UmlClassDiagram_from_type()
    {
        var diagram = typeof(List<Dictionary<string, object>>).ToUmlClassDiagram().ToMarkdown();

        this.Assent(diagram.ToString());
    }

    [Fact]
    public void can_configure_UmlClassDiagram()
    {
        var diagram = typeof(List<Dictionary<string, object>>).ToUmlClassDiagram(new UmlClassDiagramConfiguration(1))
           .ToMarkdown();

        diagram.ToString().Should()
            .NotContain(
                "ICollection~Dictionary<String, Object>~ --|> IEnumerable~Dictionary<String, Object>~ : Inheritance");
    }

}