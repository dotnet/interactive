// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Pocket.For.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests;

[LogToPocketLogger(FileNameEnvironmentVariable = "POCKETLOGGER_LOG_PATH")]
public class DirectiveTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public DirectiveTests(ITestOutputHelper output)
    {
        _disposables.Add(output.SubscribeToPocketLogger());
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    [Fact]
    public void Directives_may_be_prefixed_with_hash()
    {
        using var kernel = new CompositeKernel();

        kernel
            .Invoking(k => k.AddDirective(new KernelActionDirective("#hello"), (_, _) => Task.CompletedTask))
            .Should()
            .NotThrow();
    }

    [Theory(Skip = "TODO")]
    [InlineData("{")]
    [InlineData(";")]
    [InlineData("a")]
    [InlineData("1")]
    public void Directives_may_not_begin_with_(string value)
    {
        using var kernel = new CompositeKernel();

        var addInvalidDirective = () =>
            kernel.AddDirective(new KernelActionDirective($"{value}hello"), (_, _) => Task.CompletedTask);

        addInvalidDirective
            .Should()
            .Throw<ArgumentException>()
            .Which
            .Message
            .Should()
            .Be($"Invalid directive name \"{value}hello\". Directives must begin with \"#\".");
    }

    [Fact]
    public async Task Default_directive_handlers_are_invoked_in_the_order_in_which_they_occur_in_the_code_submission()
    {
        using var kernel = new CSharpKernel();
        var events = kernel.KernelEvents.ToSubscribedList();

        kernel.AddDirective(
            new KernelActionDirective("#!increment"),
            async (_, context) =>
            {
                await context.HandlingKernel.SubmitCodeAsync("i++;");
            });

        await kernel.SubmitCodeAsync(@"
var i = 0;
#!increment
i");

        events
            .Should()
            .ContainSingle<ReturnValueProduced>()
            .Which
            .Value
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task Custom_command_directive_handlers_are_invoked_in_the_order_in_which_they_occur_in_the_code_submission()
    {
        using var kernel = new CSharpKernel();

        kernel.AddDirective<IncrementCommand>(
            new KernelActionDirective("#!increment")
            {
                KernelCommandType = typeof(IncrementCommand),
                Parameters =
                {
                    new("--variable-name")
                }
            },
            async (increment, context) =>
            {
                await context.HandlingKernel.SubmitCodeAsync($"{increment.VariableName}++;");
            });

        var result = await kernel.SubmitCodeAsync(@"
var i = 0;
#!increment --variable-name i
i");

        result.Events
              .Should()
              .ContainSingle<ReturnValueProduced>()
              .Which
              .Value
              .Should()
              .Be(1);
    }

    [Fact]
    public async Task Custom_command_directive_handlers_can_be_invoked_by_sending_the_associated_KernelCommand_to_the_kernel_directly()
    {
        using var kernel = new CSharpKernel();

        kernel.AddDirective<IncrementCommand>(
            new KernelActionDirective("#!increment")
            {
                KernelCommandType = typeof(IncrementCommand),
                Parameters =
                {
                    new("--variable-name")
                }
            },
            async (increment, context) => { await context.HandlingKernel.SubmitCodeAsync($"{increment.VariableName}++;"); });

        await kernel.SubmitCodeAsync("var i = 0;");

        var result = await kernel.SendAsync(new IncrementCommand { VariableName = "i" });
        result.Events.Should().NotContainErrors();

        result = await kernel.SubmitCodeAsync("i");

        result.Events
              .Should()
              .ContainSingle<ReturnValueProduced>()
              .Which
              .Value
              .Should()
              .Be(1);
    }

    [Fact]
    public async Task Custom_directives_can_be_added_after_submission_parser_has_already_been_initialized()
    {
        var csharpKernel = new CSharpKernel();
        using var compositeKernel = new CompositeKernel
        {
            csharpKernel.UseValueSharing()
                        .UseImportMagicCommand()
        };
        compositeKernel.DefaultKernelName = csharpKernel.Name;

        var result = await compositeKernel.SendAsync(
                         new SubmitCode(
                             """
                             #!set --value 123 --name x
                             """));

        result.Events.Should().NotContainErrors();

        AddFruitDirectiveTo(csharpKernel);

        result = await compositeKernel.SendAsync(
                     new SubmitCode(
                         """
                         #!fruit --varieties [ "Macintosh", "Granny Smith" ] --name "apple"
                         """));

        result.Events.Should().NotContainErrors();

        result.Events.Should().ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues.Should().ContainSingle(v => v.Value.Contains("apple"));
    }

    [Fact]
    public async Task Magic_command_JSON_parsing_errors_provide_an_informative_error_message()
    {
        var csharpKernel = new CSharpKernel();

        AddFruitDirectiveTo(csharpKernel);

        var result = await csharpKernel.SendAsync(
                         new SubmitCode(
                             """
                             #!fruit --varieties { } --name "apple"
                             """));

        result.Events
              .Should()
              .ContainSingle<CommandFailed>()
              .Which
              .Message
              .Should()
              .Contain("error DNI106: Invalid JSON: The JSON value could not be converted to System.String[]. Path: $.varieties |");
    }

    private static void AddFruitDirectiveTo(Kernel csharpKernel)
    {
        var fruitDirective = new KernelActionDirective("#!fruit")
        {
            Parameters =
            [
                new KernelDirectiveParameter("--name", "The name of the fruit")
                {
                    AllowImplicitName = true,
                    Required = true
                },
                new KernelDirectiveParameter("--varieties", "The available varieties of the fruit")
                {
                    MaxOccurrences = 1000
                }
            ]
        };

        csharpKernel.AddDirective<SpecifyFruitCommand>(
            fruitDirective,
            (command, context) =>
            {
                command.Display("text/plain");
                return Task.CompletedTask;
            });
    }

    public class SpecifyFruitCommand : KernelDirectiveCommand
    {
        public SpecifyFruitCommand(string name, string[] varieties)
        {
            Name = name;
            Varieties = varieties;
        }

        public string Name { get; }

        public string[] Varieties { get; }
    }

    public class IncrementCommand : KernelCommand
    {
        public string VariableName { get; set; }
    }

    [Fact]
    public async Task Directive_parse_errors_are_displayed()
    {
        var directive = new KernelActionDirective("#!oops")
        {
            Parameters =
            {
                new("-x")
                {
                    Required = true
                }
            }
        };

        using var kernel = new CSharpKernel();

        kernel.AddDirective(directive, (command, context) => Task.CompletedTask);

        var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SubmitCodeAsync("#!oops");

        events.Should()
              .ContainSingle<CommandFailed>()
              .Which
              .Message
              .Should()
              .Be("(1,1): error DNI104: Missing required parameter '-x'");
    }

    [Fact]
    public async Task Directive_parse_errors_prevent_code_submission_from_being_run()
    {
        var command = new KernelActionDirective("#!x")
        {
            Parameters = { new("--required") { Required = true } }
        };

        using var kernel = new CSharpKernel();

        kernel.AddDirective(command, (_, _) => Task.CompletedTask);

        var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SubmitCodeAsync("#!x\n123");

        events.Should().NotContain(e => e is ReturnValueProduced);
        events.Last().Should().BeOfType<CommandFailed>();
    }

    [Theory] // https://github.com/dotnet/interactive/issues/2085
    [InlineData("[|#!unknown|]\n123")]
    [InlineData("// first line\n[|#!unknown|]\n123")]
    public async Task Unrecognized_directives_result_in_errors(string markedUpCode)
    {
        MarkupTestFile.GetPositionAndSpan(markedUpCode, out var code, out _, out var span);
        MarkupTestFile.GetLineAndColumn(markedUpCode, span.Value.Start, out var startLine, out var startCol);
        MarkupTestFile.GetLineAndColumn(markedUpCode, span.Value.End, out var endLine, out var endCol);

        var startPos = new LinePosition(startLine, startCol);
        var endPos = new LinePosition(endLine, endCol);

        var expectedPos = new LinePositionSpan(startPos, endPos);

        using var kernel = new CSharpKernel();

        var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SubmitCodeAsync(code);

        events.Should().NotContain(e => e is ReturnValueProduced);
        events.Should()
              .ContainSingle<DiagnosticsProduced>()
              .Which
              .Diagnostics
              .Should()
              .ContainSingle()
              .Which
              .LinePositionSpan
              .Should()
              .BeEquivalentTo(expectedPos);
        events.Last().Should().BeOfType<CommandFailed>();
    }

    [Fact]
    public async Task OnComplete_can_be_used_to_act_on_completion_of_commands()
    {
        var onCompleteWasCalled = false;
        using var kernel = new FakeKernel();

        kernel.AddDirective(new KernelActionDirective("#!wrap"), (_, context) =>
        {
            context.OnComplete(_ => { onCompleteWasCalled = true; });

            return Task.CompletedTask;
        });

        await kernel.SubmitCodeAsync("#!wrap");

        onCompleteWasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task OnComplete_can_be_used_to_publish_events_before_context_is_completed()
    {
        using var kernel = new FakeKernel();

        kernel.AddDirective(new KernelActionDirective("#!wrap"), (_, ctx) =>
        {
            ctx.Display("hello!");

            ctx.OnComplete(c => c.Display("goodbye!"));

            return Task.CompletedTask;
        });

        var result = await kernel.SubmitCodeAsync("#!wrap");

        result.Events
              .OfType<DisplayedValueProduced>()
              .Select(e => e.Value)
              .Should()
              .BeEquivalentSequenceTo("hello!", "goodbye!");
    }

    [Fact]
    public async Task New_directives_can_be_added_after_older_ones_have_been_evaluated()
    {
        using var kernel = new CompositeKernel { new CSharpKernel() };

        using var events = kernel.KernelEvents.ToSubscribedList();

        var oneWasCalled = false;
        var twoWasCalled = false;

        kernel.AddDirective(new KernelActionDirective("#!one"),
            (_, _) =>
            {
                oneWasCalled = true;
                return Task.CompletedTask;
            }
        );

        await kernel.SubmitCodeAsync("#!one\n123");

        events.Should().NotContainErrors();

        kernel.AddDirective(new KernelActionDirective("#!two"),
                            (_, _) =>
                            {
                                twoWasCalled = true;
                                return Task.CompletedTask;
                            }
        );

        await kernel.SubmitCodeAsync("#!two\n123");

        events.Should().NotContainErrors();
        oneWasCalled.Should().BeTrue();
        twoWasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task DirectiveCommand_is_not_referenced_by_KernelEvent_Command()
    {
        using var kernel = new FakeKernel();

        kernel.AddDirective(
            new KernelActionDirective("#!test"),
            (_, ctx) =>
            {
                ctx.Display("goodbye!");

                return Task.CompletedTask;
            }
        );

        var submitCode = new SubmitCode("#!test");

        var result = await kernel.SendAsync(submitCode);

        result.Events
              .Should()
              .ContainSingle<DisplayedValueProduced>()
              .Which
              .Command
              .Should()
              .Be(submitCode);
    }

    [Fact]
    public async Task Partial_match_does_not_invoke_incorrect_subcommand()
    {
        using var compositeKernel = new CompositeKernel();

        var firstWasCalledWith = "";
        var secondWasCalledWith = "";

        compositeKernel.AddConnectDirective(new ConnectCustomDirective(s =>
        {
            firstWasCalledWith = s;
            return Task.FromResult<Kernel>(new FakeKernel(s));
        }));
        compositeKernel.AddConnectDirective(new ConnectCustomKernelTwoDirective(s =>
        {
            secondWasCalledWith = s;
            return Task.FromResult<Kernel>(new FakeKernel(s));
        }));

        await compositeKernel.SubmitCodeAsync(
            """
            #!connect custom --kernel-name one
            """);

        await compositeKernel.SubmitCodeAsync(
            """
            #!connect custom-two --kernel-name two
            """);

        firstWasCalledWith.Should().Be("one");
        secondWasCalledWith.Should().Be("two");
    }

    public class ConnectCustomDirective : ConnectKernelDirective<ConnectCustomKernel>
    {
        private readonly Func<string, Task<Kernel>> _createKernel;

        public ConnectCustomDirective(
            Func<string, Task<Kernel>> createKernel) : base("custom", "this is the description")
        {
            ConnectedKernelDescription = "this is the description";

            _createKernel = createKernel;
        }

        public override async Task<IEnumerable<Kernel>> ConnectKernelsAsync(
            ConnectCustomKernel command,
            KernelInvocationContext context)
        {
            var kernel = await _createKernel(command.ConnectedKernelName);

            return new[] { kernel };
        }
    }

    public class ConnectCustomKernel : ConnectKernelCommand
    {
        public ConnectCustomKernel(string connectedKernelName) : base(connectedKernelName)
        {
        }
    }

    public class ConnectCustomKernelTwoDirective : ConnectKernelDirective<ConnectCustomKernelTwo>
    {
        private readonly Func<string, Task<Kernel>> _createKernel;

        public ConnectCustomKernelTwoDirective(
            Func<string, Task<Kernel>> createKernel) : base("custom-two", "this is the description")
        {
            ConnectedKernelDescription = "this is the description";

            _createKernel = createKernel;
        }

        public override async Task<IEnumerable<Kernel>> ConnectKernelsAsync(
            ConnectCustomKernelTwo command,
            KernelInvocationContext context)
        {
            var kernel = await _createKernel(command.ConnectedKernelName);

            return new[] { kernel };
        }
    }

    public class ConnectCustomKernelTwo : ConnectKernelCommand
    {
        public ConnectCustomKernelTwo(string connectedKernelName) : base(connectedKernelName)
        {
        }
    }
}
