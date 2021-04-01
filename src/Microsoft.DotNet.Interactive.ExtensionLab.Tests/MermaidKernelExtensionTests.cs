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

            string formatted = new MermaidMarkdown(@"
    graph TD
    A[Client] --> B[Load Balancer]
    B --> C[Server1]
    B --> D[Server2]").ToDisplayString(HtmlFormatter.MimeType);

            this.Assent(formatted.FixedGuid());
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

            string formatted = typeof(List<string>).ExploreWithUmlClassDiagram().ToDisplayString(HtmlFormatter.MimeType);

            this.Assent(formatted.FixedGuid());
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

            SubscribedList<KernelEvent> events = result.KernelEvents.ToSubscribedList();

            string formattedData = events
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
            var diagram = typeof(List<Dictionary<string, object>>).ToClassDiagram(new ClassDiagramConfiguration(2));

            this.Assent(diagram.ToString());
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
                .WithGraphDepth(2).ToMarkdown();

            this.Assent(diagram.ToString());
        }

        public void Dispose()
        {
            Formatter.ResetToDefault();
        }

    }
}