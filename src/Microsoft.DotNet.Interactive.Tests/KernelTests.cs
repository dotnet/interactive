// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

#if !NETFRAMEWORK
using Microsoft.DotNet.Interactive.CSharp;
#endif

namespace Microsoft.DotNet.Interactive.Tests;

public partial class KernelTests
{
    [Fact]
    public void Deferred_initialization_command_is_not_executed_prior_to_first_submission()
    {
        var receivedCommands = new List<KernelCommand>();

        using var kernel = new FakeKernel
        {
            Handle = (command, context) =>
            {
                receivedCommands.Add(command);
                return Task.CompletedTask;
            }
        };

        kernel.DeferCommand(new SubmitCode("hello"));
        kernel.DeferCommand(new SubmitCode("world!"));

        receivedCommands.Should().BeEmpty();
    }

    [Fact]
    public async Task Deferred_initialization_command_is_executed_on_first_submission()
    {
        var receivedCommands = new List<KernelCommand>();

        using var kernel = new FakeKernel
        {
            Handle = (command, context) =>
            {
                receivedCommands.Add(command);
                return Task.CompletedTask;
            }
        };

        kernel.DeferCommand(new SubmitCode("one"));
        kernel.DeferCommand(new SubmitCode("two"));

        await kernel.SendAsync(new SubmitCode("three"));

        receivedCommands
            .Select(c => c is SubmitCode submitCode ? submitCode.Code : c.ToString())
            .Should()
            .BeEquivalentSequenceTo("one", "two", "three");
    }

    [Fact]
    public async Task Split_non_referencing_directives_are_executed_in_textual_order()
    {
        var receivedCommands = new List<string>();

        using var kernel = new FakeKernel
        {
            Handle = (command, context) =>
            {
                receivedCommands.Add(((SubmitCode)command).Code);
                return Task.CompletedTask;
            }
        };

        kernel.AddDirective(new Command("#!one")
        {
            Handler = CommandHandler.Create(() =>
            {
                receivedCommands.Add("#!one");
            })
        });

        kernel.AddDirective(new Command("#!two")
        {
            Handler = CommandHandler.Create(() =>
            {
                receivedCommands.Add("#!two");
            })
        });

        var code1 = "var a = 1";
        var code2 = "var b = 2";
        var code3 = "var c = 3";
        var directive1 = "#!one";
        var directive2 = "#!two";

        await kernel.SubmitCodeAsync($@"
{code1}
{directive1}
{code2}
{directive2}
{code3}
");

        receivedCommands
            .Select(c => c.Trim())
            .Should()
            .BeEquivalentSequenceTo(
                code1,
                directive1,
                code2,
                directive2,
                code3);
    }

    [Fact]
    public async Task Middleware_is_only_executed_once_per_command()
    {
        var middeware1Count = 0;
        var middeware2Count = 0;
        var middeware3Count = 0;

        using var kernel = new FakeKernel();

        kernel.AddMiddleware(async (command, context, next) =>
        {
            middeware1Count++;
            await next(command, context);
        }, "one");
        kernel.AddMiddleware(async (command, context, next) =>
        {
            middeware2Count++;
            await next(command, context);
        }, "two");
        kernel.AddMiddleware(async (command, context, next) =>
        {
            middeware3Count++;
            await next(command, context);
        }, "three");

        await kernel.SendAsync(new SubmitCode("123"));

        middeware1Count.Should().Be(1);
        middeware2Count.Should().Be(1);
        middeware3Count.Should().Be(1);
    }

    [Fact]
    public async Task kernelEvents_sequence_completes_when_kernel_is_disposed()
    {
        var kernel = new FakeKernel();
        var events = kernel.KernelEvents.Timeout(5.Seconds()).LastOrDefaultAsync();

        kernel.Dispose();

        var lastEvent = await events;
        lastEvent.Should().BeNull();
    }

#if !NETFRAMEWORK
    [Fact]
    public async Task language_service_command_with_empty_buffer_doesnt_crash()
    {
        using var kernel = new CompositeKernel() { new CSharpKernel() };

        var request = new RequestCompletions(
            string.Empty,
            new LinePosition(0, 0),
            "csharp");

        bool fail = false;

        try
        {
            await kernel.SendAsync(request);
        }
        catch
        {
            fail = true;
        }

        fail.Should().BeFalse(because: "there were no unhandled exceptions emitted");
    }

    [Fact]
    public async Task Awaiting_a_disposed_task_does_not_deadlock()
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel()
        };

        await kernel.SendAsync(new SubmitCode("""
            using Microsoft.DotNet.Interactive;
            using Microsoft.DotNet.Interactive.Commands;
            using Microsoft.DotNet.Interactive.Events;
            using Microsoft.DotNet.Interactive.CSharp;

            var csharp2 = new CSharpKernel();
            """));

        await kernel.SendAsync(new SubmitCode("""
            var result = csharp2.SendAsync(new SubmitCode("123"));
            """));

        var result = await kernel.SendAsync(new SubmitCode("""
            (await result).Events
            """));

        result.Events.Should().ContainSingle<CommandFailed>()
              .Which.Exception.Should().BeOfType<ObjectDisposedException>();
    }

    [Fact(Skip = "later")]
    public async Task Invocation_context_does_not_cause_entanglement_between_kernels_that_do_not_share_a_scheduler()
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel()
        };

        await kernel.SendAsync(new SubmitCode("""
            using Microsoft.DotNet.Interactive;
            using Microsoft.DotNet.Interactive.Commands;
            using Microsoft.DotNet.Interactive.Events;
            using Microsoft.DotNet.Interactive.CSharp;

            var csharp2 = new CSharpKernel();
            var csharp2Events = new List<KernelEvent>();
            csharp2.KernelEvents.Subscribe(e => csharp2Events.Add(e));
            """));

        await kernel.SendAsync(new SubmitCode("""
            var result = csharp2.SendAsync(new SubmitCode("123"));
            """));

        var result = await kernel.SendAsync(new SubmitCode("""
            csharp2Events.Count
        """));

        result.Events.Should().ContainSingle<ReturnValueProduced>()
              .Which.Value.As<int>().Should().BeGreaterThan(0);
    }
#endif

    [Fact]
    public async Task it_can_handle_commands_that_submit_commands_that_are_split()
    {
        var subkernel = new FakeKernel();
        var magicCommand = new Command("#!magic");
        bool magicWasCalled = false;
        magicCommand.SetHandler(_ =>
        {
            magicWasCalled = true;
        });
        subkernel.AddDirective(magicCommand);

        subkernel.Handle = async (command, context) =>
        {
            if (command is SubmitCode submitCode)
            {
                switch (submitCode.Code)
                {
                    case "outer submission":
                        await context.HandlingKernel.RootKernel.SendAsync(new SubmitCode("""
                                #!magic
                                inner submission
                                """));

                        break;

                    case "inner submission":
                        context.Display("inner submission event", mimeTypes: "text/plain");
                        break;
                }
            }
        };

        using var kernel = new CompositeKernel
        {
            subkernel
        };

        using var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SubmitCodeAsync("outer submission");

        magicWasCalled.Should().BeTrue();

        events.Should().ContainSingle<DisplayedValueProduced>(v => v.Value.Equals("inner submission event"));
    }
}