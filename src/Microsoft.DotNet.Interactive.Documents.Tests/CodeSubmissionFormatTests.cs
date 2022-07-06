// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Documents.Tests
{
    public class CodeSubmissionFormatTests : DocumentFormatTestsBase
    {
        public InteractiveDocument ParseDib(string content)
        {
            return CodeSubmission.Parse(content, "csharp", KernelNames);
        }

        public string SerializeDib(InteractiveDocument interactive, string newLine)
        {
            return interactive.ToCodeSubmissionContent(newLine);
        }

        [Fact]
        public void empty_dib_file_parses_as_a_single_empty_cell()
        {
            var notebook = ParseDib(string.Empty);
            notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .Match<InteractiveDocumentElement>(cell =>
                    cell.Language == "csharp" &&
                    cell.Contents == string.Empty
                );
        }

        [Fact]
        public void top_level_code_without_a_language_specifier_is_assigned_the_default_language()
        {
            var notebook = ParseDib("var x = 1;");
            notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .Match<InteractiveDocumentElement>(cell =>
                    cell.Language == "csharp" &&
                    cell.Contents == "var x = 1;"
                );
        }

        [Fact]
        public void parsed_cells_can_specify_their_language_without_retaining_the_language_specifier()
        {
            var notebook = ParseDib(@"#!fsharp
let x = 1");
            notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .Match<InteractiveDocumentElement>(cell =>
                    cell.Language == "fsharp" &&
                    cell.Contents == "let x = 1"
                );
        }

        [Fact]
        public void parsed_cells_without_a_language_specifier_retain_magic_commands_and_the_default_language()
        {
            var notebook = ParseDib(@"#!probably-a-magic-command
var x = 1;");
            notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .Match<InteractiveDocumentElement>(cell =>
                    cell.Language == "csharp" &&
                    cell.Contents == "#!probably-a-magic-command\nvar x = 1;"
                );
        }

        [Fact]
        public void parsed_cells_with_a_language_specifier_retain_magic_commands()
        {
            var notebook = ParseDib(@"#!fsharp
#!probably-a-magic-command
let x = 1");
            notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .Match<InteractiveDocumentElement>(cell =>
                    cell.Language == "fsharp" &&
                    cell.Contents == "#!probably-a-magic-command\nlet x = 1"
                );
        }

        [Fact]
        public void parsed_cells_with_connect_directive_dont_cause_subsequent_cells_to_change_language()
        {
            var notebook = ParseDib(@"
#!csharp
#!connect named-pipe --kernel-name wpf --pipe-name some-pipe-name

#!csharp
#!wpf -h
");
            notebook.Elements
                .Should()
                .SatisfyRespectively(
                    cell => cell.Should().Match(_ => cell.Language == "csharp" && cell.Contents == "#!connect named-pipe --kernel-name wpf --pipe-name some-pipe-name"),
                    cell => cell.Should().Match(_ => cell.Language == "csharp" && cell.Contents == "#!wpf -h")
                );
        }

        [Fact]
        public void multiple_cells_can_be_parsed()
        {
            var notebook = ParseDib(@"#!csharp
var x = 1;
var y = 2;

#!fsharp
let x = 1
let y = 2");
            notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("csharp", "var x = 1;\nvar y = 2;"),
                    new InteractiveDocumentElement("fsharp", "let x = 1\nlet y = 2")
                });
        }

        [Fact]
        public void empty_language_cells_are_removed_when_parsing()
        {
            var notebook = ParseDib(@"#!csharp
//

#!fsharp

#!pwsh
Get-Item

#!fsharp
");
            notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("csharp", "//"),
                    new InteractiveDocumentElement("pwsh", "Get-Item")
                });
        }

        [Fact]
        public void empty_lines_are_removed_between_cells()
        {
            var notebook = ParseDib(@"


#!csharp
// first line of C#



// last line of C#





#!fsharp

// first line of F#



// last line of F#


");
            notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("csharp", "// first line of C#\n\n\n\n// last line of C#"),
                    new InteractiveDocumentElement("fsharp", "// first line of F#\n\n\n\n// last line of F#")
                });
        }

        [Theory]
        [InlineData("markdown")]
        [InlineData("md")]
        public void markdown_cells_can_be_parsed_even_though_its_not_a_kernel_language(string cellLanguage)
        {
            var notebook = ParseDib($@"
#!{cellLanguage}

This is `markdown`.
");
            notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("markdown", "This is `markdown`.")
                });
        }

        [Fact]
        public void language_aliases_are_expanded_when_parsed()
        {
            var notebook = ParseDib(@"
#!c#
// this is csharp 1

#!C#
// this is csharp 2

#!f#
// this is fsharp 1

#!F#
// this is fsharp 2

#!powershell
# this is pwsh

#!md
This is `markdown` with an alias.
");
            notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("csharp", "// this is csharp 1"),
                    new InteractiveDocumentElement("csharp", "// this is csharp 2"),
                    new InteractiveDocumentElement("fsharp", "// this is fsharp 1"),
                    new InteractiveDocumentElement("fsharp", "// this is fsharp 2"),
                    new InteractiveDocumentElement("pwsh", "# this is pwsh"),
                    new InteractiveDocumentElement("markdown", "This is `markdown` with an alias.")
                });
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void different_line_separators_are_honored_and_normalized(string newline)
        {
            var lines = new[]
            {
                "#!csharp",
                "1+1",
                "",
                "#!fsharp",
                "[1;2;3;4]",
                "|> List.sum"
            };
            var code = string.Join(newline, lines);
            var notebook = ParseDib(code);
            notebook.Elements
                .Should()
                .BeEquivalentToRespectingRuntimeTypes(new[]
                {
                    new InteractiveDocumentElement("csharp", "1+1"),
                    new InteractiveDocumentElement("fsharp", "[1;2;3;4]\n|> List.sum")
                });
        }

        [Fact]
        public void parsed_notebook_outputs_are_empty()
        {
            var notebook = ParseDib(@"
#! csharp

var x = 1;

");
            notebook.Elements
                .Should()
                .ContainSingle()
                .Which
                .Outputs
                .Should()
                .BeEmpty();
        }

        [Fact]
        public void extra_blank_lines_are_removed_from_beginning_and_end_on_save()
        {
            var cells = new List<InteractiveDocumentElement>()
            {
                new InteractiveDocumentElement("csharp", "\n\n\n\n// this is csharp\n\n\n")
            };
            var notebook = new InteractiveDocument(cells);
            var serialized = SerializeDib(notebook, "\n");
            var expectedLines = new[]
            {
                "#!csharp",
                "",
                "// this is csharp",
                ""
            };
            var expected = string.Join("\n", expectedLines);
            serialized
                .Should()
                .Be(expected);
        }

        [Fact]
        public void empty_cells_are_not_serialized()
        {
            var cells = new List<InteractiveDocumentElement>()
            {
                new InteractiveDocumentElement("csharp", ""),
                new InteractiveDocumentElement("fsharp", "// this is fsharp"),
                new InteractiveDocumentElement("csharp", "")
            };
            var notebook = new InteractiveDocument(cells);
            var serialized = SerializeDib(notebook, "\n");
            var expectedLines = new[]
            {
                "#!fsharp",
                "",
                "// this is fsharp",
                ""
            };
            var expected = string.Join("\n", expectedLines);
            serialized
                .Should()
                .Be(expected);
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void multiple_cells_are_serialized_with_appropriate_separators(string newline)
        {
            var cells = new List<InteractiveDocumentElement>()
            {
                new InteractiveDocumentElement("csharp", $"// C# line 1{newline}// C# line 2"),
                new InteractiveDocumentElement("fsharp", $"// F# line 1{newline}// F# line 2"),
                new InteractiveDocumentElement("markdown", "This is `markdown`.")
            };
            var notebook = new InteractiveDocument(cells);
            var serialized = SerializeDib(notebook, newline);
            var expectedLines = new[]
            {
                "#!csharp",
                "",
                "// C# line 1",
                "// C# line 2",
                "",
                "#!fsharp",
                "",
                "// F# line 1",
                "// F# line 2",
                "",
                "#!markdown",
                "",
                "This is `markdown`.",
                ""
            };
            var expected = string.Join(newline, expectedLines);
            serialized
                .Should()
                .Be(expected);
        }
    }
}
