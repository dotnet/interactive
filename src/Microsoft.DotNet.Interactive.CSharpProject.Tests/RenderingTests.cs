// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using HtmlAgilityPack;
using Markdig;
using Microsoft.DotNet.Interactive.CSharpProject.Markdown;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests
{
    public class RenderingTests
    {
        private readonly ITestOutputHelper _output;

        public RenderingTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Markdown_snippets_can_be_transformed_into_editor_HTML()
        {
            var packageName = "Microsoft.DotNet.Interactive.Markdown";
            var packageVersion = "1.2.3";

            var pipeline = new MarkdownPipelineBuilder()
                           .UseCodeBlockAnnotations()
                           .Build();

            var markdown = @"

```cs --editable:true
Console.WriteLine(""Hello world!"");
```
";

            var writer = new StringWriter();

            var context = new MarkdownParserContext();

            context.AddDefaultCodeBlockAnnotations(defaults =>
            {
                defaults.Package = packageName;
                defaults.PackageVersion = packageVersion;
            });

           Markdig.Markdown.ToHtml(
                markdown,
                writer,
                pipeline,
                context);

            var html = writer.ToString();

            _output.WriteLine(html);

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            var codeNode = htmlDocument.DocumentNode
                                       .SelectSingleNode("//pre/code");

            codeNode.Attributes["data-trydotnet-package"]
                    .Value
                    .Should()
                    .Be(packageName);
            codeNode.Attributes["data-trydotnet-package"]
                    .Value
                    .Should()
                    .Be(packageName);
            codeNode.Attributes["data-trydotnet-package-version"]
                    .Value
                    .Should()
                    .Be(packageVersion);
        }

        [Fact]
        public void Adds_file_name_attribute()
        {
            var fileName = "Program.cs";

            var pipeline = new MarkdownPipelineBuilder()
                           .UseCodeBlockAnnotations()
                           .Build();

            var markdown = $@"

```cs --destination-file {fileName}
Console.WriteLine(""Hello world!"");
```
";

            var writer = new StringWriter();

            var context = new MarkdownParserContext();

            Markdig.Markdown.ToHtml(
                 markdown,
                 writer,
                 pipeline,
                 context);

            var html = writer.ToString();

            _output.WriteLine(html);

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            var codeNode = htmlDocument.DocumentNode
                                       .SelectSingleNode("//pre/code");

            codeNode.Attributes["data-trydotnet-file-name"]
                    .Value
                    .Should()
                    .Be(fileName);
        }
    }
}