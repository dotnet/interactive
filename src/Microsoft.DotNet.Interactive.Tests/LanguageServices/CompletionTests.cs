// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
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

            await kernel.SendAsync(new RequestCompletions("aaa", new LinePosition(0, 3)));

            KernelEvents
                .Should()
                .ContainSingle<CompletionsProduced>()
                .Which
                .Completions
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

            await kernel.SendAsync(new RequestCompletions("aaa", new LinePosition(0, 2)));

            KernelEvents
                .Should()
                .ContainSingle<CompletionsProduced>()
                .Which
                .Completions
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

            await kernel.SendAsync(new RequestCompletions("aaa", new LinePosition(0, 3)));

            KernelEvents
                .Should()
                .ContainSingle<CompletionsProduced>()
                .Which
                .Completions
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

            await kernel.SendAsync(new RequestCompletions("aaa", new LinePosition(0, 3)));

            KernelEvents
                .Should()
                .ContainSingle<CompletionsProduced>()
                .Which
                .Completions
                .Should()
                .Contain(item => item.DisplayText == variableName);
        }

        [Theory]
        // commands
        [InlineData("[|#!c|]", "#!csharp")]
        [InlineData("[|#!|]", "#!csharp,#!who,#!whos")]
        [InlineData("[|#!w|]", "#!who,#!whos")]
        [InlineData("[|#!w|]\n", "#!who,#!whos")]
        [InlineData("[|#!w|] \n", "#!who,#!whos")]
        // options
        [InlineData("#!share [||]", "--from")]
        public void Completions_are_available_for_magic_commands(
            string markupCode,
            string expected)
        {
            var kernel = CreateKernel();

            markupCode
                .ParseMarkupCode()
                .PositionsInMarkedSpans()
                .Should()
                .ProvideCompletions(kernel)
                .Which
                .Should()
                .AllSatisfy(
                    requestCompleted =>
                        requestCompleted
                            .Completions
                            .Select(i => i.DisplayText)
                            .Should()
                            .Contain(expected.Split(","),
                                     because: $"position {requestCompleted.LinePositionSpan} should provide completions"));
        }

        [Fact]
        public void Magic_command_completions_include_magic_commands_from_all_kernels()
        {
            var markupCode = "[|#!|]";
            var expected = new[] { "#!csharp", "#!fsharp", "#!pwsh", "#!who" };

            using var kernel = new CompositeKernel
            {
                new CSharpKernel().UseWho(),
                new FSharpKernel().UseWho(),
                new PowerShellKernel()
            };

            markupCode
                .ParseMarkupCode()
                .PositionsInMarkedSpans()
                .Should()
                .ProvideCompletions(kernel)
                .Which
                .Should()
                .AllSatisfy(
                    requestCompleted =>
                        requestCompleted
                            .Completions
                            .Select(i => i.DisplayText)
                            .Should()
                            .Contain(expected,
                                     because: $"position {requestCompleted.LinePositionSpan} should provide completions"));
        }

        [Fact]
        public async Task Magic_command_completions_do_not_include_duplicates()
        {
            var cSharpKernel = new CSharpKernel();

            using var compositeKernel = new CompositeKernel
            {
                cSharpKernel
            };

            compositeKernel.DefaultKernelName = cSharpKernel.Name;

            var commandName = "#!hello";
            compositeKernel.AddDirective(new Command(commandName)
            {
                Handler = CommandHandler.Create(() => {})
            });
            cSharpKernel.AddDirective(new Command(commandName)
            {
                Handler = CommandHandler.Create(() => {})
            });

            var result = await compositeKernel.SendAsync(new RequestCompletions("#!", new LinePosition(0, 2)));

            var events = result.KernelEvents.ToSubscribedList();

            events
                .Should()
                .ContainSingle<CompletionsProduced>()
                .Which
                .Completions
                .Should()
                .ContainSingle(e => e.DisplayText == commandName);
        }

        [Theory]
        [InlineData("[|#!d|]", "#!directiveOnChild,#!directiveOnParent", Language.CSharp)]
        [InlineData("[|#!dir|]\n", "#!directiveOnChild,#!directiveOnParent", Language.CSharp)]
        [InlineData("[|#!d|]", "#!directiveOnChild,#!directiveOnParent", Language.FSharp)]
        [InlineData("[|#!dir|]\n", "#!directiveOnChild,#!directiveOnParent", Language.FSharp)]
        [InlineData("[|#!d|]", "#!directiveOnChild,#!directiveOnParent", Language.PowerShell)]
        [InlineData("[|#!dir|]\n", "#!directiveOnChild,#!directiveOnParent", Language.PowerShell)]
        public void Completions_are_available_for_magic_commands_added_at_runtime_to_child_and_parent_kernels(
            string markupCode,
            string expected,
            Language defaultLanguage)
        {
            var kernel = CreateKernel(defaultLanguage);

            var kernelToExtend = kernel.FindKernel(defaultLanguage.LanguageName());

            kernelToExtend.AddDirective(new Command("#!directiveOnChild")
            {
                Handler = CommandHandler.Create(() => { })
            });

            kernel.AddDirective(new Command("#!directiveOnParent")
            {
                Handler = CommandHandler.Create(() => { })
            });

            markupCode
                .ParseMarkupCode()
                .PositionsInMarkedSpans()
                .Should()
                .ProvideCompletions(kernel)
                .Which
                .Should()
                .AllSatisfy(
                    requestCompleted =>
                        requestCompleted
                            .Completions
                            .Select(i => i.DisplayText)
                            .Should()
                            .Contain(expected.Split(","),
                                     because: $"position {requestCompleted.LinePositionSpan} should provide completions"));
        }

        [Fact]
        public async Task does_not_work()
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

            var events = result.KernelEvents.ToSubscribedList();

            events
                .Should()
                .ContainSingle<CompletionsProduced>()
                .Which
                .Completions
                .Should()
                .Contain(item => item.DisplayText == "diego");
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
            await kernel.SendAsync(new RequestCompletions(output, new LinePosition(line, column)));

            KernelEvents
                .Should()
                .ContainSingle<CompletionsProduced>()
                .Which
                .Completions
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
            await kernel.SendAsync(new RequestCompletions(output, new LinePosition(line, column)));

            KernelEvents
                .Should()
                .ContainSingle<CompletionsProduced>()
                .Which
                .Completions
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
            await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));

            KernelEvents
                .Should()
                .ContainSingle<CompletionsProduced>()
                .Which
                .LinePositionSpan
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
            await kernel.SendAsync(new RequestCompletions(code, new LinePosition(line, character)));

            KernelEvents
                .Should()
                .ContainSingle<CompletionsProduced>()
                .Which
                .LinePositionSpan
                .Should()
                .Be(new LinePositionSpan(new LinePosition(line, 0), new LinePosition(line, 3)));
        }

        [Fact]
        public async Task Magic_command_completion_documentation_does_not_include_root_command_name()
        {
            var exeName = RootCommand.ExecutableName;

            var kernel = CreateKernel();

            var result = await kernel.SendAsync(new RequestCompletions("#!", new LinePosition(0, 2)));

            var events = result.KernelEvents.ToSubscribedList();

            events
                .Should()
                .ContainSingle<CompletionsProduced>()
                .Which
                .Completions
                .Select(i => i.Documentation)
                .Should()
                .NotContain(i => i.Contains(exeName));
        }
    }
}