// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using FluentAssertions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using System;

namespace Microsoft.DotNet.Interactive.Tests;

public class KernelInvocationContextTests
{
    [Fact]
    public async Task Current_differs_per_async_context()
    {
        var barrier = new Barrier(2);

        KernelCommand commandInTask1 = null;

        KernelCommand commandInTask2 = null;

        await Task.Run(() =>
        {
            using (var x = KernelInvocationContext.GetOrCreateAmbientContext(new SubmitCode("")))
            {
                barrier.SignalAndWait(1000);
                commandInTask1 = KernelInvocationContext.Current.Command;
            }
        });

        await Task.Run(() =>
        {
            using (KernelInvocationContext.GetOrCreateAmbientContext(new SubmitCode("")))
            {
                barrier.SignalAndWait(1000);
                commandInTask2 = KernelInvocationContext.Current.Command;
            }
        });

        commandInTask1.Should()
            .NotBe(commandInTask2)
            .And
            .NotBeNull();
    }

    [Fact]
    public async Task Parented_commands_reuse_same_context()
    {
        var barrier = new Barrier(2);
        var contextsByRootToken = new ConcurrentDictionary<string, KernelInvocationContext>(StringComparer.OrdinalIgnoreCase);
        KernelCommand commandInTask1 = null;
        KernelCommand commandInTask2 = null;

        var kernelCommand1 = new SubmitCode("");
        var kernelCommand2 = new SubmitCode("");

        KernelInvocationContext context1 = null;
        KernelInvocationContext context2 = null;

        kernelCommand2.SetToken($"{kernelCommand1.GetOrCreateToken()}.1");

        await Task.Run(() =>
        {
            context1 = KernelInvocationContext.GetOrCreateAmbientContext(kernelCommand1, contextsByRootToken);

                
                commandInTask1 = KernelInvocationContext.Current.Command;
                barrier.SignalAndWait(1000);

        });

        await Task.Run(() =>
        {
            context2 = KernelInvocationContext.GetOrCreateAmbientContext(kernelCommand2, contextsByRootToken);
            
               
                commandInTask2 = KernelInvocationContext.Current.Command;
                barrier.SignalAndWait(1000);

        });

        context1.Should().BeSameAs(context2);

    }

    [Fact]
    public async Task Middleware_can_be_used_to_emit_events_after_the_command_has_been_handled()
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel()
        };

        using var kernelEvents = kernel.KernelEvents.ToSubscribedList();

        kernel.AddMiddleware(async (command, context, next) =>
        {
            context.Publish(new DisplayedValueProduced(1, command));

            await next(command, context);

            context.Publish(new DisplayedValueProduced(3, command));
        });

        var result = await kernel.SendAsync(new SubmitCode("2"));

        var values = result.Events.OfType<DisplayEvent>()
            .Where(x => x is ReturnValueProduced || x is DisplayedValueProduced)
            .Select(v => v.Value);

        values
            .Should()
            .BeEquivalentSequenceTo(1, 2, 3);
    }

    [Fact]
    public async Task Commands_created_by_submission_splitting_do_not_publish_CommandSucceeded()
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel ( "cs1" ),
            new CSharpKernel ( "cs2" )
        };
        var kernelEvents = kernel.KernelEvents.ToSubscribedList();
        var command = new SubmitCode(@"
#!cs1
""hello""

#!cs2
""world""
");
        await kernel.SendAsync(command);
        kernelEvents.Should()
            .ContainSingle<CommandSucceeded>()
            .Which
            .Command.Should().BeSameAs(command);
    }

    [Fact]
    public async Task Commands_created_by_submission_splitting_do_not_publish_CommandFailed()
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel ( "cs1" ),
            new CSharpKernel ( "cs2" )
        };
        var kernelEvents = kernel.KernelEvents.ToSubscribedList();
        var command = new SubmitCode(@"
#!cs1
""hello""

#!cs2
error
");
        await kernel.SendAsync(command);
        kernelEvents.Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Command.Should().BeSameAs(command);
    }

    [Fact]
    public void When_Fail_is_called_CommandFailed_is_published()
    {
        var command = new SubmitCode("123");

        using var context = KernelInvocationContext.GetOrCreateAmbientContext(command);

        var events = context.KernelEvents.ToSubscribedList();

        context.Fail(command, message: "oops!");

        events.Should()
            .ContainSingle<CommandFailed>();
    }

    [Fact]
    public void When_Fail_is_called_CommandHandled_is_not_published()
    {
        var command = new SubmitCode("123");

        using var context = KernelInvocationContext.GetOrCreateAmbientContext(command);

        var events = context.KernelEvents.ToSubscribedList();

        context.Fail(command, message: "oops!");

        events.Should()
            .NotContain(e => e is CommandSucceeded);
    }

    [Fact]
    public void When_Complete_is_called_then_CommandHandled_is_published()
    {
        var command = new SubmitCode("123");

        using var context = KernelInvocationContext.GetOrCreateAmbientContext(command);

        var events = context.KernelEvents.ToSubscribedList();

        context.Complete(command);

        events.Should()
            .ContainSingle<CommandSucceeded>();
    }

    [Fact]
    public void When_Complete_is_called_then_CommandFailed_is_not_published()
    {
        var command = new SubmitCode("123");

        using var context = KernelInvocationContext.GetOrCreateAmbientContext(command);

        var events = context.KernelEvents.ToSubscribedList();

        context.Complete(command);

        events.Should()
            .NotContain(e => e is CommandFailed);
    }

    [Fact]
    public void When_Complete_is_called_then_no_further_events_are_published()
    {
        var command = new SubmitCode("123");

        using var context = KernelInvocationContext.GetOrCreateAmbientContext(command);

        var events = context.KernelEvents.ToSubscribedList();

        context.Complete(command);

        context.Publish(new ErrorProduced("oops", command));

        events.Should().NotContain(e => e is ErrorProduced);
    }

    [Fact]
    public void When_Fail_is_called_then_no_further_events_are_published()
    {
        var command = new SubmitCode("123");

        using var context = KernelInvocationContext.GetOrCreateAmbientContext(command);

        var events = context.KernelEvents.ToSubscribedList();

        context.Fail(command, message: "oops");

        context.Publish(new DisplayedValueProduced("oops", command));

        events.Should().NotContain(e => e is DisplayedValueProduced);
    }

    [Fact]
    public void When_multiple_commands_are_active_then_context_does_not_publish_CommandHandled_until_all_are_complete()
    {
        var outerSubmitCode = new SubmitCode("abc");
        using var outer = KernelInvocationContext.GetOrCreateAmbientContext(outerSubmitCode);

        var events = outer.KernelEvents.ToSubscribedList();

        var innerSubmitCode = new SubmitCode("def");
        using var inner = KernelInvocationContext.GetOrCreateAmbientContext(innerSubmitCode);

        inner.Complete(innerSubmitCode);

        events.Should().NotContain(e => e is CommandSucceeded);
    }

    [Fact]
    public void When_outer_context_is_completed_then_inner_commands_can_no_longer_be_used_to_publish_events()
    {
        using var outer = KernelInvocationContext.GetOrCreateAmbientContext(new SubmitCode("abc"));

        var events = outer.KernelEvents.ToSubscribedList();

        using var inner = KernelInvocationContext.GetOrCreateAmbientContext(new SubmitCode("def"));

        outer.Complete(outer.Command);
        inner.Publish(new ErrorProduced("oops!", inner.Command));

        events.Should().NotContain(e => e is ErrorProduced);
    }

    [Fact]
    public void When_inner_context_is_completed_then_no_further_events_can_be_published_for_it()
    {
        using var outer = KernelInvocationContext.GetOrCreateAmbientContext(new SubmitCode("abc"));

        var events = outer.KernelEvents.ToSubscribedList();

        var innerSubmitCode = new SubmitCode("def");
        using var inner = KernelInvocationContext.GetOrCreateAmbientContext(innerSubmitCode);

        inner.Complete(innerSubmitCode);

        inner.Publish(new ErrorProduced("oops!", innerSubmitCode));

        events.Should().NotContain(e => e is ErrorProduced);
    }

    [Fact]
    public void After_disposal_Current_is_null()
    {
        var context = KernelInvocationContext.GetOrCreateAmbientContext(new SubmitCode("123"));
            
        context.Dispose();

        KernelInvocationContext.Current.Should().BeNull();
    }

    [Fact]
    public void When_inner_context_fails_then_CommandFailed_is_published_for_outer_command()
    {
        using var outer = KernelInvocationContext.GetOrCreateAmbientContext(new SubmitCode("abc"));

        var events = outer.KernelEvents.ToSubscribedList();

        var innerCommand = new SubmitCode("def");
        using var inner = KernelInvocationContext.GetOrCreateAmbientContext(innerCommand);

        inner.Fail(innerCommand);

        events.Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Command
            .Should()
            .Be(outer.Command);
    }

    [Fact]
    public void When_inner_context_fails_then_no_further_events_can_be_published()
    {
        var command = new SubmitCode("abc");
        using var outer = KernelInvocationContext.GetOrCreateAmbientContext(command);

        var events = outer.KernelEvents.ToSubscribedList();

        var innerCommand = new SubmitCode("def");
        using var inner = KernelInvocationContext.GetOrCreateAmbientContext(innerCommand);

        inner.Fail(innerCommand);
        inner.Publish(new DisplayedValueProduced("oops!", command));

        events.Should().NotContain(e => e is DisplayedValueProduced);
    }
}