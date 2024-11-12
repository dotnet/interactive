// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using Assent;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Documents.Tests;

public class CodeSubmissionFormatTests : DocumentFormatTestsBase
{
    private readonly Configuration _assentConfiguration =
        new Configuration()
            .UsingExtension("dib")
            .UsingSanitiser(s => s.Replace("\r\n", "\n"))
            .SetInteractive(Debugger.IsAttached);

    public InteractiveDocument ParseDib(string content)
    {
        return CodeSubmission.Parse(content, DefaultKernelInfos);
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
                                                       cell.KernelName == "csharp" &&
                                                       cell.Contents == string.Empty);
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
                                                       cell.KernelName == "csharp" &&
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
                                                       cell.KernelName == "fsharp" &&
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
                                                       cell.KernelName == "csharp" &&
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
                                                       cell.KernelName == "fsharp" &&
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
                    cell => cell.Should().Match(_ => cell.KernelName == "csharp" && cell.Contents == "#!connect named-pipe --kernel-name wpf --pipe-name some-pipe-name"),
                    cell => cell.Should().Match(_ => cell.KernelName == "csharp" && cell.Contents == "#!wpf -h")
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
                    new InteractiveDocumentElement("var x = 1;\nvar y = 2;", "csharp"),
                    new InteractiveDocumentElement("let x = 1\nlet y = 2", "fsharp")
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
                    new InteractiveDocumentElement("//", "csharp"),
                    new InteractiveDocumentElement("Get-Item", "pwsh")
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
                    new InteractiveDocumentElement("// first line of C#\n\n\n\n// last line of C#", "csharp"),
                    new InteractiveDocumentElement("// first line of F#\n\n\n\n// last line of F#", "fsharp")
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
                    new InteractiveDocumentElement("This is `markdown`.", "markdown")
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
                    new InteractiveDocumentElement("// this is csharp 1", "csharp"),
                    new InteractiveDocumentElement("// this is csharp 2", "csharp"),
                    new InteractiveDocumentElement("// this is fsharp 1", "fsharp"),
                    new InteractiveDocumentElement("// this is fsharp 2", "fsharp"),
                    new InteractiveDocumentElement("# this is pwsh", "pwsh"),
                    new InteractiveDocumentElement("This is `markdown` with an alias.", "markdown")
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
                    new InteractiveDocumentElement("1+1", "csharp"),
                    new InteractiveDocumentElement("[1;2;3;4]\n|> List.sum", "fsharp")
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
        var notebook = new InteractiveDocument
        {
            new("\n\n\n\n// this is csharp\n\n\n", "csharp")
        };
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
        var notebook = new InteractiveDocument
        {
            new("", "csharp"),
            new("// this is fsharp", "fsharp"),
            new("", "csharp")
        };
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
        var cells = new List<InteractiveDocumentElement>
        {
            new($"// C# line 1{newline}// C# line 2", "csharp"),
            new($"// F# line 1{newline}// F# line 2", "fsharp"),
            new("This is `markdown`.", "markdown")
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

    [Fact]
    public void Default_language_can_be_specified_in_metadata()
    {
        var kernelInfo = DefaultKernelInfos;
        DefaultKernelInfos.DefaultKernelName = "fsharp";

        var metadata = new Dictionary<string, object>
        {
            ["kernelInfo"] = kernelInfo
        };

        var content = GetDibContent(metadata);

        var document = CodeSubmission.Parse(content);

        document.GetDefaultKernelName()
                .Should()
                .Be("fsharp");
    }

    [Fact]
    public void Kernel_languages_can_be_specified_in_metadata()
    {
        var kernelInfo = DefaultKernelInfos;
        kernelInfo.Add(new("mermaid"));
        kernelInfo.Add(new("javascript"));

        var metadata = new Dictionary<string, object>
        {
            ["kernelInfo"] = kernelInfo
        };

        var content = GetDibContent(metadata);

        var document = CodeSubmission.Parse(content);

        document.Elements
                .Select(e => e.KernelName)
                .Should()
                .BeEquivalentSequenceTo(new[]
                {
                    "markdown",
                    "csharp",
                    "fsharp",
                    "pwsh",
                    "javascript",
                    "mermaid",
                });
    }

    [Fact]
    public void dib_file_with_only_metadata_section_can_be_loaded()
    {
        var content = @"#!meta
{""theAnswer"":42}";
        var document = ParseDib(content);
        document
            .Metadata
            .Should()
            .ContainKey("theAnswer");
    }

    [Fact]
    public void kernel_selector_can_immediately_follow_metadata_section()
    {
        var content = @"#!meta
{""theAnswer"":42}
#!csharp
var x = 1;";
        var document = ParseDib(content);

        using var _ = new AssertionScope();

        // validate metadata
        document
            .Metadata
            .Should()
            .ContainKey("theAnswer");

        // validate content
        document
            .Elements
            .Single()
            .Contents
            .Should()
            .Be("var x = 1;");
    }

    [Fact]
    public void Metadata_section_is_not_added_as_a_document_element()
    {
        var kernelInfo = DefaultKernelInfos;
        kernelInfo.Add(new("mermaid"));
        kernelInfo.Add(new("javascript"));

        var metadata = new Dictionary<string, object>
        {
            ["kernelInfo"] = kernelInfo
        };

        var content = GetDibContent(metadata);

        var document = CodeSubmission.Parse(content);

        document.Elements
                .Select(e => e.KernelName)
                .Should()
                .NotContain("meta");
    }

    [Fact]
    public void Metadata_JSON_can_span_multiple_lines()
    {
        var dib = @"
#!meta

{
  ""someProperty"": 123
}

#!markdown

# Title

#!csharp

Console.Write(""hello"");

";

        var document = CodeSubmission.Parse(dib);

        document.Metadata
                .Should()
                .ContainKey("someProperty")
                .WhoseValue
                .Should()
                .BeOfType<JsonElement>();
    }

    [Fact]
    public async Task dib_file_can_be_round_tripped_through_read_and_write_without_the_content_changing()
    {
        var path = GetNotebookFilePath();

        var roundTrippedDib = await RoundTripDib(path);

        this.Assent(roundTrippedDib, _assentConfiguration);
    }

    private static string GetDibContent(Dictionary<string, object> metadata)
    {
        if (metadata is null)
        {
            throw new ArgumentNullException(nameof(metadata));
        }

        var serializedMetadata = JsonSerializer.Serialize(metadata, App.ParserServer.ParserServerSerializer.JsonSerializerOptions);

        return $@"#!meta

{serializedMetadata}

#!markdown

* Markdown code

#!csharp

// C# code

#!fsharp

// F# code

#!pwsh

# PowerShell code

#!javascript

// JavaScript code

#!mermaid

%% Mermaid code
";
    }

    [Fact]
    public void Input_tokens_are_parsed_from_dib_files()
    {
        var dib = """
          #!value --from-file @input:"Enter a filename" --name myfile
          """;

        var document = CodeSubmission.Parse(dib);

        document.GetInputFields(ParseDirectiveLine)
                .Should()
                .ContainSingle()
                .Which
                .ValueName
                .Should()
                .Be("myfile");
    }

    [Fact]
    public void Password_tokens_are_parsed_from_dib_files()
    {
        var dib = """
                  #!do-stuff --password @password:"TOPSECRET"
                  """;

        var document = CodeSubmission.Parse(dib);

        document.GetInputFields(ParseDirectiveLine)
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeEquivalentTo(new InputField("TOPSECRET", "password"));
    }

    [Fact]
    public void When_an_input_field_name_is_repeated_then_only_one_is_created_in_the_document()
    {
        var dib = """

                  #!do-stuff @password:the-password
                  #!do-more-stuff @password:the-password

                  """;

        var document = CodeSubmission.Parse(dib);

        document.GetInputFields(ParseDirectiveLine)
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeEquivalentTo(new InputField("the-password", "password"));
    }

    [Fact]
    public void When_using_set_magic_then_input_field_names_are_set_using_name_option()
    {
        var dib = """
            #!set --name value_name --value @input:"This is the prompt" 
            """;

        var document = CodeSubmission.Parse(dib);

        document.GetInputFields(ParseDirectiveLine)
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeEquivalentTo(new InputField("value_name", "text"));
    }
    
    [Fact]
    public void When_using_set_magic_then_password_field_names_are_set_using_name_option()
    {
        var dib = """
            #!set --name value_name --value @password:"This is the prompt" 
            """;

        var document = CodeSubmission.Parse(dib);

        document.GetInputFields(ParseDirectiveLine)
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .BeEquivalentTo(new InputField("value_name", "password"));
    }

    private async Task<string> RoundTripDib(string filePath)
    {
        var expectedContent = await File.ReadAllTextAsync(filePath);

        var inputDoc = CodeSubmission.Parse(expectedContent);

        var resultContent = inputDoc.ToCodeSubmissionContent();

        return resultContent;
    }

    private string GetNotebookFilePath([CallerMemberName] string testName = null) =>
        Path.Combine(
            Path.GetDirectoryName(PathToCurrentSourceFile()),
            $"{GetType().Name}.{testName}.approved.dib");
}