// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.CodeAnalysis.CSharp;
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
        [InlineData("[|#!c|]", "#!csharp")]
        [InlineData("[|#!w|]", "#!who,#!whos")]
        [InlineData("[|#!w|]\n", "#!who,#!whos")]
        [InlineData("[|#!w|] \n", "#!who,#!whos")]
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
        [InlineData("[|#!t|]", "#!two,#!twilight", null)]
        [InlineData("[|#!tw|]\n", "#!two,#!twilight", null)]
        [InlineData("[|#!t|]", "#!two,#!twilight", Language.CSharp)]
        [InlineData("[|#!tw|]\n", "#!two,#!twilight", Language.CSharp)]
        [InlineData("[|#!t|]", "#!two,#!twilight", Language.FSharp)]
        [InlineData("[|#!tw|]\n", "#!two,#!twilight", Language.FSharp)]
        [InlineData("[|#!t|]", "#!two,#!twilight", Language.PowerShell)]
        [InlineData("[|#!tw|]\n", "#!two,#!twilight", Language.PowerShell)]
        public async Task Completions_are_available_for_magic_commands_added_at_runtime(
            string markupCode,
            string expected, 
            Language? language)
        {
            MarkupTestFile.GetSpan(markupCode, out var code, out var span);

            var sourceText = SourceText.From(code);

            var kernel = CreateKernel(language?? Language.CSharp);

            using var _ = new AssertionScope();

            _output.WriteLine($"Checking positions from {span.Start} to {span.End}");

            KernelBase kernelToExtend = kernel;
            if (language != null)
            {
                kernelToExtend = kernel.FindKernel(language.Value.LanguageName()) as KernelBase;
            }

            kernelToExtend.AddDirective(new Command("#!two")
            {
                Handler = CommandHandler.Create(() => { })
            });


            kernel.AddDirective(new Command("#!twilight")
            {
                Handler = CommandHandler.Create(() => { })
            });


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
    }
}