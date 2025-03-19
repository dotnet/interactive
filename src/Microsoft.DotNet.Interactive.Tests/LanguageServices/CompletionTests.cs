// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;

#pragma warning disable 8509
namespace Microsoft.DotNet.Interactive.Tests.LanguageServices;

[TestClass]
public partial class CompletionTests : LanguageKernelTestBase
{
    public CompletionTests(TestContext output) : base(output)
    {
    }

    [TestMethod]
    [DataRow(Language.FSharp)]
    [DataRow(Language.CSharp)]
    public async Task Completions_are_available_for_symbols_declared_in_the_previous_submission(Language language)
    {
        var variableName = "aaaaaaa";

        var declarationSubmission = language switch
        {
            Language.CSharp => $"var {variableName} = 123;",
            Language.FSharp => $"let {variableName} = 123"
        };

        var kernel = CreateKernel(language);

        await kernel.SubmitCodeAsync(declarationSubmission);

        var result = await kernel.SendAsync(new RequestCompletions("aaa", new LinePosition(0, 3)));

        result.Events
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Should()
            .Contain(item => item.DisplayText == variableName);
    }

    [TestMethod]
    [DataRow(Language.FSharp)]
    [DataRow(Language.CSharp)]
    public async Task Completions_are_available_for_symbols_declared_in_a_submission_before_the_previous_submission(Language language)
    {
        var variableName = "aaaaaaa";

        var submissions = language switch
        {
            Language.CSharp => new[]
            {
                $"var {variableName} = 123;",
                "var bbbbb = 456;"
            },
            Language.FSharp => new[]
            {
                $"let {variableName} = 123",
                "let bbbbb = 456"
            }
        };

        var kernel = CreateKernel(language);

        foreach (var submission in submissions)
        {
            await kernel.SubmitCodeAsync(submission);
        }

        var result = await kernel.SendAsync(new RequestCompletions("aaa", new LinePosition(0, 2)));

        result.Events
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Should()
            .Contain(item => item.DisplayText == variableName);
    }

    [TestMethod]
    [DataRow(Language.FSharp)]
    [DataRow(Language.CSharp)]
    public async Task Completions_are_available_for_symbols_members(Language language)
    {
        var declaration = language switch
        {
            Language.CSharp => new SubmitCode("var fileInfo = new System.IO.FileInfo(\"temp.file\");"),
            Language.FSharp => new SubmitCode("let fileInfo = new System.IO.FileInfo(\"temp.file\")")
        };

        var kernel = CreateKernel(language);
        await kernel.SendAsync(declaration);

        MarkupTestFile.GetLineAndColumn("fileInfo.$$", out var useInput, out var line, out var column);
        var result = await kernel.SendAsync(new RequestCompletions(useInput, new LinePosition(line, column)));

        result.Events
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Should()
            .Contain(item => item.DisplayText == "AppendText");
    }

    [TestMethod]
    [DataRow(Language.FSharp)]
    [DataRow(Language.CSharp)]
    public async Task Completions_are_available_for_symbols_declared_in_the_previous_submission_ending_in_a_trailing_expression(Language language)
    {
        var variableName = "aaaaaaa";

        var submission = language switch
        {
            Language.CSharp => $"var {variableName} = 123;\n{variableName}",
            Language.FSharp => $"let {variableName} = 123\n{variableName}"
        };

        var kernel = CreateKernel(language);

        await kernel.SubmitCodeAsync(submission);

        var result = await kernel.SendAsync(new RequestCompletions("aaa", new LinePosition(0, 3)));

        result.Events
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Should()
            .Contain(item => item.DisplayText == variableName);
    }

    [TestMethod]
    [DataRow(Language.FSharp)]
    [DataRow(Language.CSharp)]
    public async Task Completions_are_available_for_symbols_declared_in_a_submission_before_the_previous_one_ending_in_a_trailing_expression(Language language)
    {
        var variableName = "aaaaaaa";

        var submissions = language switch
        {
            Language.CSharp => new[] { $"var {variableName} = 123;\n{variableName}", "1 + 2" },
            Language.FSharp => new[] { $"let {variableName} = 123\n{variableName}", "1 + 2" }
        };

        var kernel = CreateKernel(language);

        foreach (var submission in submissions)
        {
            await kernel.SubmitCodeAsync(submission);
        }

        var result = await kernel.SendAsync(new RequestCompletions("aaa", new LinePosition(0, 3)));

        result.Events
              .Should()
              .ContainSingle<CompletionsProduced>()
              .Which
              .Completions
              .Should()
              .Contain(item => item.DisplayText == variableName);
    }

    [TestMethod]
    public async Task Subsequent_completion_commands_produce_the_expected_results()
    {
        var kernel = CreateKernel();

        var firstCodeSubmission = new SubmitCode("var jon = new { Name = \"Jon\" };");

        var secondCodeSubmission = new SubmitCode("var diego = new { Name = \"Diego\", AwesomeFriend = jon };");

        await kernel.SendAsync(firstCodeSubmission);
        await kernel.SendAsync(secondCodeSubmission);

        var firstCompletionRequest = new RequestCompletions("j", new LinePosition(0, 1));

        var secondCompletionRequest = new RequestCompletions("die", new LinePosition(0, 3));

        await kernel.SendAsync(firstCompletionRequest);

        var result = await kernel.SendAsync(secondCompletionRequest);

        result.Events
              .Should()
              .ContainSingle<CompletionsProduced>()
              .Which
              .Completions
              .Should()
              .Contain(item => item.DisplayText == "diego");
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public async Task completion_commands_produce_values_after_normalizing_the_request(Language language)
    {
        var variableName = "aaaaaaa";

        var declarationSubmission = language switch
        {
            Language.CSharp => $"var {variableName} = 123;",
            Language.FSharp => $"let {variableName} = 123"
        };

        var kernel = CreateKernel(language);

        await kernel.SubmitCodeAsync(declarationSubmission);

        var completionCode = string.Join("\r\n", new[]
        {
            "", // blank line to force offsets to be wrong
            "#!time",
            "aaa$$"
        });
        MarkupTestFile.GetLineAndColumn(completionCode, out var output, out var line, out var column);
        var result = await kernel.SendAsync(new RequestCompletions(output, new LinePosition(line, column)));

        result.Events
              .Should()
              .ContainSingle<CompletionsProduced>()
              .Which
              .Completions
              .Should()
              .Contain(item => item.DisplayText == variableName);
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public async Task completion_commands_have_offsets_normalized_after_switching_to_the_same_language(Language language)
    {
        var variableName = "aaaaaaa";

        var declarationSubmission = language switch
        {
            Language.CSharp => $"var {variableName} = 123;",
            Language.FSharp => $"let {variableName} = 123"
        };

        var kernel = CreateKernel(language);

        await kernel.SubmitCodeAsync(declarationSubmission);

        var completionCode = string.Join("\r\n", new[]
        {
            "", // blank line to force offsets to be wrong
            $"#!{language.LanguageName()}",
            "aaa$$"
        });
        MarkupTestFile.GetLineAndColumn(completionCode, out var output, out var line, out var column);
        await kernel.SendAsync(new RequestCompletions(output, new LinePosition(line, column)));

        KernelEvents
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Should()
            .Contain(item => item.DisplayText == variableName);
    }

    [TestMethod]
    public async Task completion_commands_and_events_have_offsets_normalized_when_switching_languages()
    {
        // switch to PowerShell from an F# kernel/cell
        using var kernel = CreateCompositeKernel(Language.FSharp);
        var fullMarkupCode = string.Join("\r\n", new[]
        {
            "let x = 1",
            "#!pwsh",
            "Get-$$"
        });

        MarkupTestFile.GetLineAndColumn(fullMarkupCode, out var code, out var line, out var character);
        await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));

        KernelEvents
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .LinePositionSpan
            .Should()
            .Be(new LinePositionSpan(new LinePosition(line, 0), new LinePosition(line, 4)));
    }

    [TestMethod]
    public async Task magic_command_completion_commands_and_events_have_offsets_normalized_when_the_submission_was_parsed_and_split()
    {
        using var kernel = CreateKernel(Language.CSharp);
        var fullMarkupCode = @"
var x = 1;
var y = x + 2;
#!w$$
";

        MarkupTestFile.GetLineAndColumn(fullMarkupCode, out var code, out var line, out var character);
        await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));

        KernelEvents
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .LinePositionSpan
            .Should()
            .Be(new LinePositionSpan(new LinePosition(line, 0), new LinePosition(line, 3)));
    }

    [TestMethod]
    [DataRow(Language.CSharp, "System.Environment.Command$$Line", "Gets the command line for this process.", IgnoreMessage = "Disabled pending https://github.com/dotnet/interactive/issues/2637")]
    public async Task completion_doc_comments_can_be_loaded_from_bcl_types(Language language, string markupCode, string expectedCompletionSubstring)
    {
        using var kernel = CreateKernel(language);

        MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);
        await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));

        KernelEvents
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Should()
            .ContainSingle(ci => !string.IsNullOrEmpty(ci.Documentation) && ci.Documentation.Contains(expectedCompletionSubstring));
    }

    [TestMethod]
    [DataRow(Language.CSharp, "/// <summary>Adds two numbers.</summary>\nint Add(int a, int b) => a + b;", "Ad$$", "Adds two numbers.", IgnoreMessage = "Disabled pending https://github.com/dotnet/interactive/issues/2637")]
    [DataRow(Language.FSharp, "/// Adds two numbers.\nlet add a b = a + b", "ad$$", "Adds two numbers.")]
    public async Task completion_doc_comments_can_be_loaded_from_source_in_a_previous_submission(Language language, string previousSubmission, string markupCode, string expectedCompletionSubString)
    {
        using var kernel = CreateKernel(language);

        await SubmitCode(kernel, previousSubmission);

        MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);
        await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));

        KernelEvents
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Should()
            .ContainSingle(ci => !string.IsNullOrEmpty(ci.Documentation) && ci.Documentation.Contains(expectedCompletionSubString));
    }

    [TestMethod]
    [DataRow(Language.CSharp, IgnoreMessage = "Disabled pending https://github.com/dotnet/interactive/issues/2637")]
    [DataRow(Language.FSharp)]
    public async Task completion_contains_doc_comments_from_individually_referenced_assemblies_with_xml_files(Language language)
    {
        using var assembly = new TestAssemblyReference("Project", "netstandard2.0", "Program.cs", @"
public class C
{
    /// <summary>This is the answer.</summary>
    public static int TheAnswer => 42;
}
");
        var assemblyPath = await assembly.BuildAndGetPathToAssembly();

        var assemblyReferencePath = language switch
        {
            Language.CSharp => assemblyPath,
            Language.FSharp => assemblyPath.Replace("\\", "\\\\")
        };

        using var kernel = CreateKernel(language);

        await SubmitCode(kernel, $"#r \"{assemblyReferencePath}\"");

        var markupCode = "C.TheAns$$";

        MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);
        await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));

        KernelEvents
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Should()
            .ContainSingle(ci => !string.IsNullOrEmpty(ci.Documentation) && ci.Documentation.Contains("This is the answer."));
    }

    [TestMethod]
    [Ignore("Disabled pending https://github.com/dotnet/interactive/issues/2637")]
    public async Task csharp_completions_can_read_doc_comments_from_nuget_packages_after_forcing_the_assembly_to_load()
    {
        using var kernel = CreateKernel(Language.CSharp);

        await SubmitCode(kernel, "#r \"nuget: Newtonsoft.Json, 13.0.1\"");

        // The following line forces the assembly and the doc comments to be loaded
        await SubmitCode(kernel, "var _unused = Newtonsoft.Json.JsonConvert.Null;");

        var markupCode = "Newtonsoft.Json.JsonConvert.Nu$$";

        MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);
        await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));

        KernelEvents
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Should()
            .ContainSingle(ci => !string.IsNullOrEmpty(ci.Documentation) && ci.Documentation.Contains("Represents JavaScript's null as a string. This field is read-only."));
    }

    [TestMethod]
    [Ignore("https://github.com/dotnet/interactive/issues/1071  N.b., the preceeding test can be deleted when this one is fixed.")]
    public async Task csharp_completions_can_read_doc_comments_from_nuget_packages()
    {
        using var kernel = CreateKernel(Language.CSharp);

        await SubmitCode(kernel, "#r \"nuget: Newtonsoft.Json, 13.0.1\"");

        var markupCode = "Newtonsoft.Json.JsonConvert.Nu$$";

        MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);
        await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));

        KernelEvents
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Should()
            .ContainSingle(ci => !string.IsNullOrEmpty(ci.Documentation) && ci.Documentation.Contains("Represents JavaScript's null as a string. This field is read-only."));
    }

    [TestMethod]
    public async Task fsharp_completions_can_read_doc_comments_from_nuget_packages()
    {
        using var kernel = CreateKernel(Language.FSharp);

        await SubmitCode(kernel, "#r \"nuget: Newtonsoft.Json, 13.0.1\"");

        var markupCode = "Newtonsoft.Json.JsonConvert.Nu$$";

        MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);
        await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));

        KernelEvents
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Should()
            .ContainSingle(ci => !string.IsNullOrEmpty(ci.Documentation) && ci.Documentation.Contains("Represents JavaScript's null as a string. This field is read-only."));
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public async Task Property_completions_are_returned_as_plain_text(Language language)
    {
        var kernel = CreateKernel(language);

        var markupCode = "Console.Ou$$";

        MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);

        await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));

        KernelEvents
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Should()
            .Contain(item =>
                item.DisplayText == "Out" &&
                item.InsertText == "Out" &&
                item.InsertTextFormat == null);
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public async Task Method_completions_are_returned_as_a_snippet(Language language)
    {
        var kernel = CreateKernel(language);

        var markupCode = "Console.Wri$$";

        MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);

        await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));

        KernelEvents
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Should()
            .Contain(item =>
                item.DisplayText == "WriteLine" &&
                item.InsertText == "WriteLine($1)" &&
                item.InsertTextFormat == InsertTextFormat.Snippet);
    }

    [TestMethod]
    public async Task FSharp_module_functions_are_returned_as_plain_text()
    {
        var kernel = CreateKernel(Language.FSharp);

        var markupCode = "[1;2;3] |> List.ma$$";

        MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);

        await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));

        KernelEvents
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Should()
            .Contain(item =>
                item.DisplayText == "map" &&
                item.InsertText == "map" &&
                item.InsertTextFormat == null);
    }

    [TestMethod]
    public async Task CSharp_generic_method_completions_are_returned_as_a_snippet()
    {
        // in general F# prefers to infer generic types, not specify them

        var kernel = CreateKernel(Language.CSharp);

        var markupCode = "System.Array.Emp$$";

        MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);

        await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));

        KernelEvents
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Should()
            .Contain(item =>
                item.DisplayText == "Empty<>" &&
                item.InsertText == "Empty<$1>($2)" &&
                item.InsertTextFormat == InsertTextFormat.Snippet);
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public async Task Non_generic_type_completions_are_returned_as_plain_text(Language language)
    {
        var kernel = CreateKernel(language);

        var markupCode = "System.Cons$$";

        MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);

        await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));

        KernelEvents
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Should()
            .Contain(item =>
                item.DisplayText == "Console" &&
                item.InsertText == "Console" &&
                item.InsertTextFormat == null);
    }

    [TestMethod]
    public async Task CSharp_generic_type_completions_are_returned_as_a_snippet()
    {
        // in general F# prefers to infer generic types, not specify them

        var kernel = CreateKernel(Language.CSharp);

        var markupCode = "System.Collections.Generic.IEnu$$";

        MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);

        await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));

        KernelEvents
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Should()
            .Contain(item =>
                item.DisplayText == "IEnumerable<>" &&
                item.InsertText == "IEnumerable<$1>" &&
                item.InsertTextFormat == InsertTextFormat.Snippet);
    }
}