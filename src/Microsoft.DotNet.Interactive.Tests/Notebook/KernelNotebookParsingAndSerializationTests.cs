// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Notebook;
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

        [Fact]
        public async Task composite_kernel_can_parse_notebooks()
        {
            using var kernel = CreateCompositeKernel();

            var notebookText = @"
#!csharp
var x = 1;
";
            var notebookBytes = Encoding.UTF8.GetBytes(notebookText);

            await kernel.SendAsync(new ParseNotebook("notebook.dib", notebookBytes, ".NET"));
            
            KernelEvents
                .Should()
                .ContainSingle<NotebookParsed>()
                .Which
                .Notebook
                .Cells
                .Should()
                .ContainSingle()
                .Which
                .Contents
                .Should()
                .Be("var x = 1;");
        }

        [Fact]
        public async Task composite_kernel_can_serialize_notebooks()
        {
            using var kernel = CreateCompositeKernel();

            var notebook = new NotebookDocument(new[]
            {
                new NotebookCell("csharp", "var x = 1;")
            });

            await kernel.SendAsync(new SerializeNotebook("notebook.dib", notebook, "\r\n", ".NET"));

            var expectedLines = new[]
            {
                "#!csharp",
                "",
                "var x = 1;",
                ""
            };
            var expectedText = string.Join("\r\n", expectedLines);

            KernelEvents
                .Should()
                .ContainSingle<NotebookSerialized>()
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
