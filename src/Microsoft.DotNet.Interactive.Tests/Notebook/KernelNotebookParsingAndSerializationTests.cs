// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests.Notebook
{
    public class KernelNotebookParsingAndSerializationTests : LanguageKernelTestBase
    {
        public KernelNotebookParsingAndSerializationTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Theory]
        [InlineData("interactive.dib")]
        [InlineData("interactive.dotnet-interactive")]
        public async Task composite_kernel_can_parse_interactive_documents(string fileName)
        {
            using var kernel = CreateCompositeKernel();

            var notebookText = @"
#!csharp
var x = 1;
";
            var notebookBytes = Encoding.UTF8.GetBytes(notebookText);

            await kernel.SendAsync(new ParseInteractiveDocument(fileName, notebookBytes, ".NET"));
            
            KernelEvents
                .Should()
                .ContainSingle<InteractiveDocumentParsed>()
                .Which
                .Document
                .Elements
                .Should()
                .ContainSingle()
                .Which
                .Contents
                .Should()
                .Be("var x = 1;");
        }

        [Fact]
        public async Task composite_kernel_can_parse_jupter_notebook()
        {
            using var kernel = CreateCompositeKernel();

            var notebookText = @"
{
  ""cells"": [
    {
      ""cell_type"": ""code"",
      ""execution_count"": 1,
      ""metadata"": {
        ""dotnet_interactive"": {
          ""language"": ""csharp""
        }
      },
      ""source"": [
        ""var x = 1;""
      ],
      ""outputs"": []
    }
  ],
  ""metadata"": {
    ""kernelspec"": {
      ""display_name"": "".NET (C#)"",
      ""language"": ""C#"",
      ""name"": "".net-csharp""
    },
    ""language_info"": {
      ""file_extension"": "".cs"",
      ""mimetype"": ""text/x-csharp"",
      ""name"": ""C#"",
      ""pygments_lexer"": ""csharp"",
      ""version"": ""8.0""
    }
  },
  ""nbformat"": 4,
  ""nbformat_minor"": 4
}
";
            var notebookBytes = Encoding.UTF8.GetBytes(notebookText);

            await kernel.SendAsync(new ParseInteractiveDocument("notebook.ipynb", notebookBytes, ".NET"));

            KernelEvents
                .Should()
                .ContainSingle<InteractiveDocumentParsed>()
                .Which
                .Document
                .Elements
                .Should()
                .ContainSingle()
                .Which
                .Contents
                .Should()
                .Be("var x = 1;");
        }

        [Theory]
        [InlineData("interactive.dib")]
        [InlineData("interactive.dotnet-interactive")]
        public async Task composite_kernel_can_serialize_notebooks(string fileName)
        {
            using var kernel = CreateCompositeKernel();

            var notebook = new Documents.InteractiveDocument(new[]
            {
                new InteractiveDocumentElement("csharp", "var x = 1;")
            });

            await kernel.SendAsync(new SerializeInteractiveDocument(fileName, notebook, "\n", ".NET"));

            var expectedLines = new[]
            {
                "#!csharp",
                "",
                "var x = 1;",
                ""
            };
            var expectedText = string.Join("\n", expectedLines);

            KernelEvents
                .Should()
                .ContainSingle<InteractiveDocumentSerialized>()
                .Which
                .RawData
                .AsString() // passing throught via this helper to make a test failure easier to identify
                .Should()
                .Be(expectedText);
        }
    }

    internal static class TestStringExtensions
    {
        public static string AsString(this byte[] rawData)
        {
            return Encoding.UTF8.GetString(rawData);
        }
    }
}
