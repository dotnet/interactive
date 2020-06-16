// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable 8509
namespace Microsoft.DotNet.Interactive.Tests.LanguageServices
{
    public class CompletionTests : LanguageKernelTestBase
    {
        private readonly ITestOutputHelper _output;

        public CompletionTests(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(Language.FSharp)]
        [InlineData(Language.CSharp)]
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

            await kernel.SendAsync(new RequestCompletion("aaa", new LinePosition(0, 3)));

            KernelEvents
                .Should()
                .ContainSingle<CompletionRequestCompleted>()
                .Which
                .CompletionList
                .Should()
                .Contain(item => item.DisplayText == variableName);
        }

        [Theory]
        [InlineData(Language.FSharp)]
        [InlineData(Language.CSharp)]
        public async Task Completions_are_available_for_symbols_declared_in_a_submission_before_the_previous_submission(Language language)
        {
            var variableName = "aaaaaaa";

            var submissions = language switch
            {
                Language.CSharp => new[]
                {
                    $"var {variableName} = 123;",
                    $"var bbbbb = 456;"
                },
                Language.FSharp => new[]
                {
                    $"let {variableName} = 123",
                    $"let bbbbb = 456"
                }
            };

            var kernel = CreateKernel(language);

            foreach (var submission in submissions)
            {
                await kernel.SubmitCodeAsync(submission);
            }

            await kernel.SendAsync(new RequestCompletion("aaa", new LinePosition(0, 2)));

            KernelEvents
                .Should()
                .ContainSingle<CompletionRequestCompleted>()
                .Which
                .CompletionList
                .Should()
                .Contain(item => item.DisplayText == variableName);
        }

        [Theory]
        [InlineData(Language.FSharp)]
        [InlineData(Language.CSharp)]
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

            await kernel.SendAsync(new RequestCompletion("aaa", new LinePosition(0, 3)));

            KernelEvents
                .Should()
                .ContainSingle<CompletionRequestCompleted>()
                .Which
                .CompletionList
                .Should()
                .Contain(item => item.DisplayText == variableName);
        }

        [Theory]
        [InlineData(Language.FSharp)]
        [InlineData(Language.CSharp)]
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

            await kernel.SendAsync(new RequestCompletion("aaa", new LinePosition(0, 3)));

            KernelEvents
                .Should()
                .ContainSingle<CompletionRequestCompleted>()
                .Which
                .CompletionList
                .Should()
                .Contain(item => item.DisplayText == variableName);
        }

        [Theory]
        // commands
        [InlineData("[|#!c|]", "#!csharp")]
        [InlineData("[|#!|]", "#!csharp,#!fsharp,#!pwsh,#!who,#!whos")]
        [InlineData("[|#!w|]", "#!who,#!whos")]
        [InlineData("[|#!w|]\n", "#!who,#!whos")]
        [InlineData("[|#!w|] \n", "#!who,#!whos")]
        // options
        [InlineData("#!share [||]", "--from")]
        public async Task Completions_are_available_for_magic_commands(
            string markupCode,
            string expected)
        {
            MarkupTestFile.GetSpan(markupCode, out var code, out var span);

            var sourceText = SourceText.From(code);

            var kernel = CreateKernel();

            using var _ = new AssertionScope();

            _output.WriteLine($"Checking positions from {span.Start} to {span.End}");

            foreach (var position in Enumerable.Range(span.Start, span.Length + 1))
            {
                var linePosition = sourceText.Lines.GetLinePosition(position);

                var result = await kernel.SendAsync(new RequestCompletion(code, linePosition));

                var events = result.KernelEvents.ToSubscribedList();

                events
                    .Should()
                    .ContainSingle<CompletionRequestCompleted>()
                    .Which
                    .CompletionList
                    .Select(i => i.DisplayText)
                    .Should()
                    .Contain(expected.Split(","),
                             because: $"position {position} should provide completions");
            }
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
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
            await kernel.SendAsync(new RequestCompletion(output, new LinePosition(line, column)));

            KernelEvents
                .Should()
                .ContainSingle<CompletionRequestCompleted>()
                .Which
                .CompletionList
                .Should()
                .Contain(item => item.DisplayText == variableName);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
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
            await kernel.SendAsync(new RequestCompletion(output, new LinePosition(line, column)));

            KernelEvents
                .Should()
                .ContainSingle<CompletionRequestCompleted>()
                .Which
                .CompletionList
                .Should()
                .Contain(item => item.DisplayText == variableName);
        }

        [Fact]
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
            await kernel.SendAsync(new RequestCompletion(code, new LinePosition(line, character)));

            KernelEvents
                .Should()
                .ContainSingle<CompletionRequestCompleted>()
                .Which
                .Range
                .Should()
                .Be(new LinePositionSpan(new LinePosition(line, 0), new LinePosition(line, 4)));
        }

        [Fact]
        public async Task magic_command_completion_commands_and_events_have_offsets_normalized_when_the_submission_was_parsed_and_split()
        {
            using var kernel = CreateKernel(Language.CSharp);
            var fullMarkupCode = @"
var x = 1;
var y = x + 2;
#!w$$
";

            MarkupTestFile.GetLineAndColumn(fullMarkupCode, out var code, out var line, out var character);
            await kernel.SendAsync(new RequestCompletion(code, new LinePosition(line, character)));

            KernelEvents
                .Should()
                .ContainSingle<CompletionRequestCompleted>()
                .Which
                .Range
                .Should()
                .Be(new LinePositionSpan(new LinePosition(line, 0), new LinePosition(line, 3)));
        }

        [Fact]
        public async Task Magic_command_completion_documentation_does_not_include_root_command_name()
        {
            var exeName = RootCommand.ExecutableName;

            var kernel = CreateKernel();

            var result = await kernel.SendAsync(new RequestCompletion("#!", new LinePosition(0, 2)));

            var events = result.KernelEvents.ToSubscribedList();

            events
                .Should()
                .ContainSingle<CompletionRequestCompleted>()
                .Which
                .CompletionList
                .Select(i => i.Documentation)
                .Should()
                .NotContain(i => i.Contains(exeName));
        }
    }
}