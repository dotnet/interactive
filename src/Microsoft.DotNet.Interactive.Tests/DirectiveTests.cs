// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
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
        // FIX: (Directives_may_not_begin_with_) consider separate types for subcommands vs top-level directives
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
        var events = kernel.KernelEvents.ToSubscribedList();

        kernel.AddDirective<IncrementCommand>(
            new KernelActionDirective("#!increment")
            {
                DeserializeAs = typeof(IncrementCommand),
                Parameters =
                {
                    new("--variable-name")
                }
            },
            async (increment, context) =>
            {
                await context.HandlingKernel.SubmitCodeAsync($"{increment.VariableName}++;");
            });

        await kernel.SubmitCodeAsync(@"
var i = 0;
#!increment --variable-name i
i");

        events
            .Should()
            .ContainSingle<ReturnValueProduced>()
            .Which
            .Value
            .Should()
            .Be(1);
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
        MarkupTestFile.GetLine(markedUpCode, span.Value.Start, out var line);
        var startPos = new LinePosition(line, span.Value.Start);
        var endPos = new LinePosition(line, span.Value.End);

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

        if (kernel is null)
        {
            throw new ArgumentNullException(nameof(kernel));
        }

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
}
