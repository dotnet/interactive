// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests.LanguageServices
{
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
                                .Completions.Select(i => i.DisplayText)
                                      .Should()
                                      .Contain(expected.Split(","),
                                               because: $"position {requestCompleted.LinePositionSpan} should provide completions"));
            }

            [Fact]
            public void Insertion_range_is_correct_for_option_completions()
            {
                var kernel = CreateKernel();

                "#!share fr[||]"
                    .ParseMarkupCode()
                    .PositionsInMarkedSpans()
                    .Should()
                    .ProvideCompletions(kernel)
                    .Which
                    .Should()
                    .AllSatisfy(c =>
                                    c.LinePositionSpan
                                     .Should()
                                     .BeEquivalentTo(new LinePositionSpan(
                                                         new LinePosition(0, 8),
                                                         new LinePosition(0, 10))));
            }

            [Fact]
            public void Insertion_range_is_correct_for_command_completions()
            {
                var kernel = CreateKernel();

                "#!wh[||]"
                    .ParseMarkupCode()
                    .PositionsInMarkedSpans()
                    .Should()
                    .ProvideCompletions(kernel)
                    .Which
                    .Should()
                    .AllSatisfy(c =>
                                    c.LinePositionSpan
                                     .Should()
                                     .BeEquivalentTo(new LinePositionSpan(
                                                         new LinePosition(0, 0),
                                                         new LinePosition(0, 4))));
            }

            [Fact]
            public void Completions_include_magic_commands_from_all_kernels()
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
            public void Inner_symbol_completions_do_not_include_top_level_symbols()
            {
                var kernel = CreateKernel();

                "#!share [| |]"
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
                var kernel = CreateCompositeKernel(defaultLanguage);

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
}