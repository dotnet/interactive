// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable 8509
namespace Microsoft.DotNet.Interactive.Tests.LanguageServices
{
    public class HoverTextTests : LanguageKernelTestBase
    {
        public HoverTextTests(ITestOutputHelper output) : base(output)
        {
        }

        private Task<KernelCommandResult> SendHoverRequest(Kernel kernel, string code, int line, int character)
        {
            var command = new RequestHoverText(code, new LinePosition(line, character));
            return kernel.SendAsync(command);
        }

        [Fact]
        public async Task hover_on_unsupported_language_service_returns_nothing()
        {
            using var kernel = new FakeKernel();

            var result = await SendHoverRequest(kernel, "code", 0, 0);

            result.KernelEvents
                  .ToSubscribedList()
                  .Should()
                  .NotContain(kv => kv is HoverTextProduced);
        }

        [Theory]
        [InlineData(Language.CSharp, "var x = 12$$34;", "text/markdown", "readonly struct System.Int32")]
        [InlineData(Language.FSharp, "let f$$oo = 12", "text/markdown", "```fsharp\nval foo : int\n```\n\n----\n*Full name: foo*")]
        public async Task hover_request_returns_expected_result(Language language, string markupCode, string expectedMimeType, string expectedContent)
        {
            using var kernel = CreateKernel(language);

            MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);
            var commandResult = await SendHoverRequest(kernel, code, line, character);

            commandResult
                .KernelEvents
                .ToSubscribedList()
                .Should()
                .ContainSingle<HoverTextProduced>()
                .Which
                .Content
                .Should()
                .ContainEquivalentOf(new FormattedValue(expectedMimeType, expectedContent));
        }

        [Theory]
        [InlineData(Language.CSharp, "var x = 1; // hovering$$ in a comment")]
        [InlineData(Language.FSharp, "let x = 1 // hovering$$ in a comment")]
        public async Task invalid_hover_request_returns_no_result(Language language, string markupCode)
        {
            using var kernel = CreateKernel(language);

            MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);
            var commandResult = await SendHoverRequest(kernel, code, line, character);

            commandResult
                .KernelEvents
                .ToSubscribedList()
                .Should()
                .NotContain(kv => kv.GetType().IsSubclassOf(typeof(HoverTextProduced)));
        }

        [Theory]
        [InlineData(Language.CSharp, "var x = 1; // hovering past the end of the line", 0, 200)]
        [InlineData(Language.CSharp, "var x = 1; // hovering on a non-existent line", 10, 2)]
        [InlineData(Language.FSharp, "let x = 1 // hovering past the end of the line", 0, 200)]
        [InlineData(Language.FSharp, "let x = 1 // hovering on a non-existent line", 10, 2)]
        public async Task out_of_bounds_hover_request_returns_no_result(Language language, string code, int line, int character)
        {
            using var kernel = CreateKernel(language);

            var commandResult = await SendHoverRequest(kernel, code, line, character);

            commandResult
                .KernelEvents
                .ToSubscribedList()
                .Should()
                .NotContain(kv => kv.GetType().IsSubclassOf(typeof(HoverTextProduced)));
        }

        [Theory]
        [InlineData(Language.CSharp, "var one = 1;", "Console.WriteLine(o$$ne)", "text/markdown", "(field) int one")]
        [InlineData(Language.FSharp, "let one = 1", "printfn \"%a\" o$$ne", "text/markdown", "```fsharp\nval one : int\n```\n\n----\n*Full name: one*")]
        public async Task language_service_methods_run_deferred_commands(Language language, string deferredCode, string markupCode, string expectedMimeType, string expectedContent)
        {
            // declare a variable in deferred code
            using var kernel = CreateKernel(language);
            kernel.DeferCommand(new SubmitCode(deferredCode));

            // send the actual language service request that depends on the deferred code
            MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);
            var commandResult = await SendHoverRequest(kernel, code, line, character);

            commandResult
                .KernelEvents
                .ToSubscribedList()
                .Should()
                .ContainSingle<HoverTextProduced>()
                .Which
                .Content
                .Should()
                .ContainEquivalentOf(new FormattedValue(expectedMimeType, expectedContent));
        }

        [Theory]
        [InlineData(Language.CSharp, "/// <summary>Adds two numbers.</summary>\nint Add(int a, int b) => a + b;", "Ad$$d(1, 2)", "Adds two numbers.")]
        [InlineData(Language.FSharp, "/// Adds two numbers.\nlet add a b = a + b", "ad$$d 1 2", "Adds two numbers.")]
        public async Task hover_text_doc_comments_can_be_loaded_from_source_in_a_previous_submission(Language language, string previousSubmission, string markupCode, string expectedHoverTextSubString)
        {
            using var kernel = CreateKernel(language);

            await SubmitCode(kernel, previousSubmission);

            MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);
            var commandResult = await SendHoverRequest(kernel, code, line, character);

            var events = commandResult.KernelEvents.ToSubscribedList();

            events
                .Should()
                .ContainSingle<HoverTextProduced>()
                .Which
                .Content
                .Should()
                .ContainSingle(fv => fv.Value.Contains(expectedHoverTextSubString));
        }

        [Theory]
        [InlineData(Language.CSharp, "var s = new Sample$$Class();")]
        [InlineData(Language.FSharp, "let s = Sample$$Class()")]
        public async Task hover_text_can_read_doc_comments_from_individually_referenced_assemblies_with_xml_files(Language language, string markupCode)
        {
            using var assembly = new TestAssemblyReference("Project", "netstandard2.0", "Program.cs", @"
public class SampleClass
{
    /// <summary>A sample class constructor.</summary>
    public SampleClass() { }
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

            MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);
            var commandResult = await SendHoverRequest(kernel, code, line, character);

            var events = commandResult.KernelEvents.ToSubscribedList();

            events
                .Should()
                .ContainSingle<HoverTextProduced>()
                .Which
                .Content
                .Should()
                .ContainSingle(fv => fv.Value.Contains("A sample class constructor."));
        }

        [Fact]
        public async Task csharp_hover_text_can_read_doc_comments_from_nuget_packages_after_forcing_the_assembly_to_load()
        {
            using var kernel = CreateKernel(Language.CSharp);

            await SubmitCode(kernel, "#r \"nuget: Newtonsoft.Json, 12.0.3\"");

            // The following line forces the assembly and the doc comments to be loaded
            await SubmitCode(kernel, "var _unused = Newtonsoft.Json.JsonConvert.Null;");

            var markupCode = "Newtonsoft.Json.JsonConvert.Nu$$ll";

            MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);
            var commandResult = await SendHoverRequest(kernel, code, line, character);

            var events = commandResult.KernelEvents.ToSubscribedList();

            events
                .Should()
                .ContainSingle<HoverTextProduced>()
                .Which
                .Content
                .Should()
                .ContainSingle(fv => fv.Value.Contains("Represents JavaScript's null as a string. This field is read-only."));
        }

        [Fact(Skip = "https://github.com/dotnet/interactive/issues/1071  N.b., the preceeding test can be deleted when this one is fixed.")]
        public async Task csharp_hover_text_can_read_doc_comments_from_nuget_packages()
        {
            using var kernel = CreateKernel(Language.CSharp);

            await SubmitCode(kernel, "#r \"nuget: Newtonsoft.Json, 12.0.3\"");

            var markupCode = "Newtonsoft.Json.JsonConvert.Nu$$ll";

            MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);
            var commandResult = await SendHoverRequest(kernel, code, line, character);

            var events = commandResult.KernelEvents.ToSubscribedList();

            events
                .Should()
                .ContainSingle<HoverTextProduced>()
                .Which
                .Content
                .Should()
                .ContainSingle(fv => fv.Value.Contains("Represents JavaScript's null as a string. This field is read-only."));
        }

        [Fact]
        public async Task fsharp_hover_text_can_read_doc_comments_from_nuget_packages()
        {
            using var kernel = CreateKernel(Language.FSharp);

            await SubmitCode(kernel, "#r \"nuget: Newtonsoft.Json, 12.0.3\"");

            var markupCode = "Newtonsoft.Json.JsonConvert.Nu$$ll";

            MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);
            var commandResult = await SendHoverRequest(kernel, code, line, character);

            var events = commandResult.KernelEvents.ToSubscribedList();

            events
                .Should()
                .ContainSingle<HoverTextProduced>()
                .Which
                .Content
                .Should()
                .ContainSingle(fv => fv.Value.Contains("Represents JavaScript's `null` as a string. This field is read-only."));
        }

        [Theory]
        [InlineData(Language.CSharp, "Console.Write$$Line();", "text/markdown", "void Console.WriteLine() (+ 17 overloads)")]
        [InlineData(Language.FSharp, "ex$$it 0", "text/markdown", "```fsharp\nval exit: \n   exitcode: int \n          -> 'T\n```\n\n----\nExit the current hardware isolated process, if security settings permit,\n otherwise raise an exception. Calls `System.Environment.Exit`.\n\n`exitcode`: The exit code to use.\n\n**Generic parameters**\n\n* `'T` is `obj`\n\n----\n*Full name: Microsoft.FSharp.Core.Operators.exit*\n\n----\n*Assembly: FSharp.Core*")]
        public async Task hover_text_commands_have_offsets_normalized_after_magic_commands(Language language, string markupCode, string expectedMimeType, string expectedContent)
        {
            using var kernel = CreateKernel(language);

            var fullMarkupCode = string.Join("\r\n", new[]
            {
                "", // blank like to force offsets to be wrong
                "#!time", // prepend with magic commands to make line offsets wrong
                markupCode
            });

            MarkupTestFile.GetLineAndColumn(fullMarkupCode, out var code, out var line, out var character);
            var commandResult = await SendHoverRequest(kernel, code, line, character);

            commandResult
                .KernelEvents
                .ToSubscribedList()
                .Should()
                .ContainSingle<HoverTextProduced>()
                .Which
                .Content
                .Should()
                .ContainSingle(fv => fv.MimeType == expectedMimeType && fv.Value == expectedContent);
        }

        [Theory]
        [InlineData(Language.CSharp, "Console.Write$$Line();", "text/markdown", "void Console.WriteLine() (+ 17 overloads)")]
        [InlineData(Language.FSharp, "ex$$it 0", "text/markdown", "```fsharp\nval exit: \n   exitcode: int \n          -> 'T\n```\n\n----\nExit the current hardware isolated process, if security settings permit,\n otherwise raise an exception. Calls `System.Environment.Exit`.\n\n`exitcode`: The exit code to use.\n\n**Generic parameters**\n\n* `'T` is `obj`\n\n----\n*Full name: Microsoft.FSharp.Core.Operators.exit*\n\n----\n*Assembly: FSharp.Core*")]
        public async Task hover_text_commands_have_offsets_normalized_after_switching_to_the_same_language(Language language, string markupCode, string expectedMimeType, string expectedContent)
        {
            using var kernel = CreateKernel(language);

            var fullMarkupCode = string.Join("\r\n", new[]
            {
                "", // blank line to force offsets to be wrong
                $"#!{language.LanguageName()}", // 'switch' to the same language
                markupCode
            });

            MarkupTestFile.GetLineAndColumn(fullMarkupCode, out var code, out var line, out var character);
            var commandResult = await SendHoverRequest(kernel, code, line, character);

            commandResult
                .KernelEvents
                .ToSubscribedList()
                .Should()
                .ContainSingle<HoverTextProduced>()
                .Which
                .Content
                .Should()
                .ContainSingle(fv => fv.MimeType == expectedMimeType && fv.Value == expectedContent);
        }

        [Fact]
        public async Task hover_text_commands_and_events_have_offsets_normalized_when_switching_languages()
        {
            // switch to C# from an F# kernel/cell
            using var kernel = CreateCompositeKernel(Language.FSharp);
            var fullMarkupCode = string.Join("\r\n", new[]
            {
                "let x = 1",
                "#!csharp",
                "Console.Write$$Line()"
            });

            MarkupTestFile.GetLineAndColumn(fullMarkupCode, out var code, out var line, out var character);
            var commandResult = await SendHoverRequest(kernel, code, line, character);

            commandResult
                .KernelEvents
                .ToSubscribedList()
                .Should()
                .ContainSingle<HoverTextProduced>()
                .Which
                .LinePositionSpan
                .Should()
                .Be(new LinePositionSpan(new LinePosition(line, 8), new LinePosition(line, 17)));
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task csharp_hover_text_is_returned_for_shadowing_variables(Language language)
        {
            SubmitCode declaration = null;
            SubmitCode shadowingDeclaration = null;
            using var kernel = CreateKernel(language);
            string expected = "";
            switch (language)
            {
                case Language.CSharp:
                    declaration = new SubmitCode("var identifier = 1234;");
                    shadowingDeclaration = new SubmitCode("var identifier = \"one-two-three-four\";");
                    expected = "(field) string identifier";
                    break;
                case Language.FSharp:
                    declaration = new SubmitCode("let identifier = 1234");
                    shadowingDeclaration = new SubmitCode("let identifier = \"one-two-three-four\"");
                    expected = "```fsharp\nval identifier : string\n```\n\n----\n*Full name: identifier*";
                    break;

            }
            await kernel.SendAsync(declaration); 

            await kernel.SendAsync(shadowingDeclaration); 

            var markupCode = "ident$$ifier";

            MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var column);

            var commandResult = await SendHoverRequest(kernel, code, line, column);

            commandResult
                .KernelEvents
                .ToSubscribedList()
                .Should()
                .ContainSingle<HoverTextProduced>()
                .Which
                .Content
                .Should()
                .ContainSingle(fv => fv.Value == expected);
        }
    }
}
