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

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests
{
    public class MermaidKernelExtensionTests : IDisposable
    {
        private readonly Configuration _configuration;

        public MermaidKernelExtensionTests()
        {
            _configuration = new Configuration()
                .UsingExtension("txt")
                .SetInteractive(Debugger.IsAttached);
        }

        [Fact]
        public async Task adds_mermaid_kernel()
        {
            using CompositeKernel kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective(),
            };

            var extension = new MermaidKernelExtension();

            await extension.OnLoadAsync(kernel);

            kernel.ChildKernels
                .Should()
                .ContainSingle(k => k is MermaidKernel);
        }

        [Fact]
        public async Task registers_html_formatter_for_MermaidMarkdown()
        {
            using CompositeKernel kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective(),
            };

            var extension = new MermaidKernelExtension();

            await extension.OnLoadAsync(kernel);

            var markdown = @"graph TD
    A[Client] --> B[Load Balancer]
    B --> C[Server1]
    B --> D[Server2]";

            var formatted = new MermaidMarkdown(markdown).ToDisplayString(HtmlFormatter.MimeType);
            var doc = new HtmlDocument();
            doc.LoadHtml(formatted.FixedGuid());
            var scriptNode = doc.DocumentNode.SelectSingleNode("//div/script");
            var renderTarget = doc.DocumentNode.SelectSingleNode("//div[@id='00000000000000000000000000000000']");
            using var _ = new AssertionScope();

            scriptNode.Should().NotBeNull();
            scriptNode.InnerText.Should()
                .Contain(markdown);
            scriptNode.InnerText.Should()
                .Contain("configureRequireFromExtension('Mermaid','1.0.0')(['Mermaid/mermaidapi'], (mermaid) => {");

            renderTarget.Should().NotBeNull();
        }

        [Fact]
        public async Task registers_html_formatter_for_explorer()
        {
            using CompositeKernel kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective(),
            };

            var extension = new MermaidKernelExtension();

            await extension.OnLoadAsync(kernel);

            var explorer = typeof(List<string>).ExploreWithUmlClassDiagram();
            var formatted = explorer.ToDisplayString(HtmlFormatter.MimeType);
            var markdown = explorer.ToMarkdown().ToString();

            var doc = new HtmlDocument();
            doc.LoadHtml(formatted.FixedGuid());
            var scriptNode = doc.DocumentNode.SelectSingleNode("//div/script");
            var renderTarget = doc.DocumentNode.SelectSingleNode("//div[@id='00000000000000000000000000000000']");
            
            using var _ = new AssertionScope();

            scriptNode.Should().NotBeNull();
            scriptNode.InnerText.Should()
                .Contain(markdown);
            scriptNode.InnerText.Should()
                .Contain("configureRequireFromExtension('Mermaid','1.0.0')(['Mermaid/mermaidapi'], (mermaid) => {");

            renderTarget.Should().NotBeNull();
        }


        [Fact]
        public async Task mermaid_kernel_handles_SubmitCode()
        {
            using CompositeKernel kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective(),
            };

            var extension = new MermaidKernelExtension();

            await extension.OnLoadAsync(kernel);

            KernelCommandResult result = await kernel.SubmitCodeAsync(@"
#!mermaid
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

            this.Assent(formattedData.FixedGuid(), _configuration);
        }

        [Fact]
        public async Task can_use_extension_methods_from_the_kernel_extension()
        {
            using CompositeKernel kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective(),
            };

            var extension = new MermaidKernelExtension();

            await extension.OnLoadAsync(kernel);

            await kernel.SendAsync(new SubmitCode($@"#r ""{typeof(MermaidKernelExtension).Assembly.Location}""", "csharp"));

            var result = await kernel.SendAsync( new SubmitCode(@"
using System;

typeof(List<string>).ExploreWithUmlClassDiagram().Display();
", "csharp"));

            var events = result.KernelEvents.ToSubscribedList();

            var formattedData = events
                .OfType<DisplayedValueProduced>()
                .Single()
                .FormattedValues
                .Single(fm => fm.MimeType == HtmlFormatter.MimeType)
                .Value;

            this.Assent(formattedData.FixedGuid(), _configuration);
        }

        [Fact]
        public void can_generate_class_diagram_from_type()
        {
            var diagram = typeof(JsonElement).ToClassDiagram();

            this.Assent(diagram.ToString());
        }

        [Fact]
        public void can_generate_class_diagram_from_generic_type()
        {
            var diagram = typeof(List<Dictionary<string,object>>).ToClassDiagram();

            this.Assent(diagram.ToString());
        }


        [Fact]
        public void can_generate_class_diagram_to_a_specified_depth()
        {
            var diagram = typeof(List<Dictionary<string, object>>).ToClassDiagram(new ClassDiagramConfiguration(1));

            diagram.ToString().Should()
                .NotContain(
                    "ICollection~Dictionary<String, Object>~ --|> IEnumerable~Dictionary<String, Object>~ : Inheritance");
        }

        [Fact]
        public void can_explore_a_type_with_UmlClassDiagram()
        {
            var diagram = typeof(List<Dictionary<string, object>>).ExploreWithUmlClassDiagram().ToMarkdown();

            this.Assent(diagram.ToString());
        }

        [Fact]
        public void can_configure_UmlClassDiagramExplorer()
        {
            var diagram = typeof(List<Dictionary<string, object>>).ExploreWithUmlClassDiagram()
                .WithGraphDepth(1).ToMarkdown();
            
            diagram.ToString().Should()
                .NotContain(
                    "ICollection~Dictionary<String, Object>~ --|> IEnumerable~Dictionary<String, Object>~ : Inheritance");
        }

        public void Dispose()
        {
            Formatter.ResetToDefault();
        }
    }
}