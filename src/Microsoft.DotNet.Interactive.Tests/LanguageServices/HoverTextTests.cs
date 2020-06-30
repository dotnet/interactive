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
        [InlineData(Language.CSharp, "Console.Write$$Line();", "text/markdown", "void Console.WriteLine() (+ 17 overloads)")]
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
                .ContainEquivalentOf(new FormattedValue(expectedMimeType, expectedContent));
        }

        [Theory]
        [InlineData(Language.CSharp, "Console.Write$$Line();", "text/markdown", "void Console.WriteLine() (+ 17 overloads)")]
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
                .ContainEquivalentOf(new FormattedValue(expectedMimeType, expectedContent));
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
    }
}