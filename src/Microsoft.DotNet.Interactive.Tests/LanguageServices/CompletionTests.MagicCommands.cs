// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests.LanguageServices;

public partial class CompletionTests
{
    public class MagicCommands : LanguageKernelTestBase
    {
        public MagicCommands(ITestOutputHelper output) : base(output)
        {
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
        [InlineData("#!csharp [||]", "--help")]
        // subcommands
        [InlineData("#!connect [||]", "--help,signalr")]
        public async Task Completions_are_available_for_magic_commands(
            string markupCode,
            string expected)
        {
            var kernel = CreateKernel();
            kernel.AddKernelConnector(new ConnectSignalRCommand());

            var completions = await markupCode
                .ParseMarkupCode()
                .PositionsInMarkedSpans()
                .Should()
                .ProvideCompletionsAsync(kernel);
            completions.Which
                .Should()
                .AllSatisfy(
                    requestCompleted =>
                        requestCompleted
                            .Completions.Select(i => i.DisplayText)
                            .Should()
                            .Contain(expected.Split(","),
                                because: $"position {requestCompleted.LinePositionSpan} should provide completions"));
        }

        [Theory]
        // commands
        [InlineData("#!sha[||]", "Get a value from one kernel and create a copy (or a reference if the kernels are in the same process) in another.")]
        // options
        [InlineData("#!share --fr[||]", "--from*ValueSource*The name of the kernel")]
        // subcommands
        [InlineData("#!connect signa[||]", "Connects to a kernel using SignalR*--hub-url*The URL of the SignalR hub")]
        public async Task Completion_documentation_is_available_for_magic_commands(
            string markupCode,
            string expected)
        {
            var kernel = CreateKernel();
            var fakeKernel = new FakeKernel("ValueSource");
            fakeKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestValue)));
            fakeKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestValueInfos)));
                
            kernel.Add(fakeKernel);
            kernel.AddKernelConnector(new ConnectSignalRCommand());

            var completions = await markupCode
                .ParseMarkupCode()
                .PositionsInMarkedSpans()
                .Should()
                .ProvideCompletionsAsync(kernel);
            completions.Which
                .Should()
                .ContainSingle()
                .Which
                .Completions
                .Should()
                .ContainSingle()
                .Which
                .Documentation
                .Should()
                .Match($"*{expected}*");
        }

        [Fact]
        public async Task Insertion_range_is_correct_for_option_completions()
        {
            var kernel = CreateCompositeKernel();

            var completions = await "#!share fr[||]"
                .ParseMarkupCode()
                .PositionsInMarkedSpans()
                .Should()
                .ProvideCompletionsAsync(kernel);
            completions.Which
                .Should()
                .AllSatisfy(c =>
                    c.LinePositionSpan
                        .Should()
                        .BeEquivalentToRespectingRuntimeTypes(new LinePositionSpan(
                            new LinePosition(0, 8),
                            new LinePosition(0, 10))));
        }

        [Fact]
        public async Task Insertion_range_is_correct_for_command_completions()
        {
            var kernel = CreateCompositeKernel();

            var completions = await "#!wh[||]"
                .ParseMarkupCode()
                .PositionsInMarkedSpans()
                .Should()
                .ProvideCompletionsAsync(kernel);
            completions.Which
                .Should()
                .AllSatisfy(c =>
                    c.LinePositionSpan
                        .Should()
                        .BeEquivalentToRespectingRuntimeTypes(new LinePositionSpan(
                            new LinePosition(0, 0),
                            new LinePosition(0, 4))));
        }

        [Fact]
        public async Task Completions_include_magic_commands_from_all_kernels()
        {
            var markupCode = "[|#!|]";
            var expected = new[] { "#!csharp", "#!fsharp", "#!pwsh", "#!who" };

            using var kernel = new CompositeKernel
            {
                new CSharpKernel().UseWho(),
                new FSharpKernel().UseWho(),
                new PowerShellKernel()
            };

            var completions = await markupCode
                .ParseMarkupCode()
                .PositionsInMarkedSpans()
                .Should()
                .ProvideCompletionsAsync(kernel);
            completions
                .Subject
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

        [Theory]
        [InlineData("#!share [| |]")]
        [InlineData("#!connect [| |]")]
        public async Task Inner_symbol_completions_do_not_include_top_level_symbols(string markupCode)
        {
            var kernel = CreateCompositeKernel();
            kernel.AddKernelConnector(new ConnectSignalRCommand());

            var completions = await markupCode
                .ParseMarkupCode()
                .PositionsInMarkedSpans()
                .Should()
                .ProvideCompletionsAsync(kernel);
            completions
                .Subject
                .Should()
                .AllSatisfy(
                    requestCompleted =>
                        requestCompleted
                            .Completions
                            .Select(i => i.DisplayText)
                            .Should()
                            .NotContain(c => c.Contains("#!")));
        }

        [Fact]
        public async Task Completions_do_not_include_duplicates()
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
                Handler = CommandHandler.Create(() => { })
            });
            cSharpKernel.AddDirective(new Command(commandName)
            {
                Handler = CommandHandler.Create(() => { })
            });

            var result = await compositeKernel.SendAsync(new RequestCompletions("#!", new LinePosition(0, 2)));

            result.Events
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
        public async Task Completions_are_available_for_magic_commands_added_at_runtime_to_child_and_parent_kernels(
            string markupCode,
            string expected,
            Language defaultLanguage)
        {
            var kernel = CreateCompositeKernel(defaultLanguage);

            var kernelToExtend = kernel.FindKernelByName(defaultLanguage.LanguageName());

            kernelToExtend.AddDirective(new Command("#!directiveOnChild")
            {
                Handler = CommandHandler.Create(() => { })
            });

            kernel.AddDirective(new Command("#!directiveOnParent")
            {
                Handler = CommandHandler.Create(() => { })
            });

            var completions = await markupCode
                .ParseMarkupCode()
                .PositionsInMarkedSpans()
                .Should()
                .ProvideCompletionsAsync(kernel);
            completions.Which
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
        public async Task Magic_command_completion_documentation_does_not_include_root_command_name()
        {
            var exeName = RootCommand.ExecutableName;

            var kernel = CreateCompositeKernel();

            var result = await kernel.SendAsync(new RequestCompletions("#!", new LinePosition(0, 2)));

            result.Events
                  .Should()
                  .ContainSingle<CompletionsProduced>()
                  .Which
                  .Completions
                  .Select(i => i.Documentation)
                  .Should()
                  .NotContain(i => i.Contains(exeName));
        }

        [Fact]
        public async Task Share_suggests_kernel_names()
        {
            var kernel = CreateCompositeKernel();

            var shareFrom = "#!share --from ";

            var result = await kernel.SendAsync(new RequestCompletions(shareFrom, new LinePosition(0, shareFrom.Length)));

            result.Events
                  .Should()
                  .ContainSingle<CompletionsProduced>()
                  .Which
                  .Completions
                  .Select(i => i.DisplayText)
                  .Should()
                  .Contain(new[] { "fsharp", "pwsh" });
        }

        [Fact]
        public async Task Set_suggests_kernel_qualified_variable_name()
        {
            var kernel = CreateCompositeKernel();
            await kernel.SubmitCodeAsync("var x = 123;");

            var shareFrom = "#!set --name y --value ";

            var result = await kernel
                               .FindKernelByName("fsharp")
                               .SendAsync(new RequestCompletions(shareFrom, new LinePosition(0, shareFrom.Length)));

            result.Events
                  .Should()
                  .ContainSingle<CompletionsProduced>()
                  .Which
                  .Completions
                  .Select(i => i.DisplayText)
                  .Should()
                  .Contain(new[] { "@csharp:x" });
        }

        [Fact]
        public async Task Share_suggests_variable_names()
        {
            var kernel = CreateCompositeKernel();

            var variableName = "aaaaaa";
            await kernel.SendAsync(
                new SubmitCode($"var {variableName} = 123;",
                    targetKernelName: "csharp"));

            var shareFrom = "#!share --from csharp ";

            var result = await kernel.SendAsync(
                new RequestCompletions(
                    shareFrom,
                    new LinePosition(0, shareFrom.Length),
                    targetKernelName: "fsharp"));

            result.Events
                  .Should()
                  .ContainSingle<CompletionsProduced>()
                  .Which
                  .Completions
                  .Select(i => i.DisplayText)
                  .Should()
                  .Contain(variableName);
        }
    }
}