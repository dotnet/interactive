// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.LanguageService;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class LanguageServiceTests : LanguageKernelTestBase
    {
        public LanguageServiceTests(ITestOutputHelper output) : base(output)
        {
        }

        private Task<IKernelCommandResult> SendHoverRequest(KernelBase kernel, string code, int line, int character)
        {
            var command = new RequestHoverTextCommand(RequestHoverTextCommand.MakeDataUriFromContents(code), new Position(line, character));
            return kernel.SendAsync(command);
        }

        [Fact]
        public async Task hover_on_unsupported_language_service_returns_nothing()
        {
            using var kernel = new FakeKernel();

            var commandResult = await SendHoverRequest(kernel, "code", 0, 0);

            commandResult
                .KernelEvents
                .ToSubscribedList()
                .Should()
                .ContainSingle<LanguageServiceNoResultProduced>();
        }

        [Theory]
        [InlineData(Language.CSharp, "var x = 1234;", 0, 10, "readonly struct System.Int32")]
        public async Task hover_request_returns_expected_result(Language language, string code, int line, int character, string expected)
        {
            using var kernel = CreateKernel(language);

            var commandResult = await SendHoverRequest(kernel, code, line, character);

            commandResult
                .KernelEvents
                .ToSubscribedList()
                .Should()
                .ContainSingle<LanguageServiceHoverResponseProduced>()
                .Which
                .Contents
                .Value
                .Should()
                .Contain(expected);
        }

        [Theory]
        [InlineData(Language.CSharp, "var x = 1; // hovering in a comment", 0, 20)]
        [InlineData(Language.CSharp, "var x = 1; // hovering past the end of the line", 0, 200)]
        [InlineData(Language.CSharp, "var x = 1; // hovering on a negative character", 0, -1)]
        [InlineData(Language.CSharp, "var x = 1; // hovering on a non-existent line", 10, 2)]
        [InlineData(Language.CSharp, "var x = 1; // hovering on a negative line", -1, 2)]
        public async Task invalid_hover_request_returns_no_result(Language language, string code, int line, int character)
        {
            using var kernel = CreateKernel(language);

            var commandResult = await SendHoverRequest(kernel, code, line, character);

            commandResult
                .KernelEvents
                .ToSubscribedList()
                .Should()
                .NotContain(kv => kv.GetType() == typeof(LanguageServiceHoverResponseProduced));
        }

        [Theory]
        [InlineData(Language.CSharp, "var one = 1;", "Console.WriteLine(one)", 0, 20, "(field) int one")]
        public async Task language_service_methods_run_deferred_commands(Language language, string deferredCode, string actualCode, int line, int character, string expected)
        {
            // declare a variable in deferred code
            using var kernel = CreateKernel(language);
            kernel.DeferCommand(new SubmitCode(deferredCode));

            // send the actual language service request that depends on the deferred code
            var commandResult = await SendHoverRequest(kernel, actualCode, line, character);

            commandResult
                .KernelEvents
                .ToSubscribedList()
                .Should()
                .ContainSingle<LanguageServiceHoverResponseProduced>()
                .Which
                .Contents
                .Value
                .Should()
                .Contain(expected);
        }
    }
}
