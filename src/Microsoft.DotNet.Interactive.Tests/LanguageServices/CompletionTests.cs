// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
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
        public CompletionTests(ITestOutputHelper output) : base(output)
        {
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
    }
}