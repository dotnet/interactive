// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests.Connection;

public class ObservableCommandAndEventReceiverTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly BlockingCollection<CommandOrEvent> _messageQueue;

    public ObservableCommandAndEventReceiverTests()
    {
        _messageQueue = new BlockingCollection<CommandOrEvent>();
        _disposables.Add(_messageQueue);
    }

    public void Dispose() => _disposables.Dispose();

    [Fact]
    public void Read_is_not_called_until_first_subscriber_subscribes()
    {
        var queue = new Queue<CommandOrEvent>();

        queue.Enqueue(new CommandOrEvent(new RequestKernelInfo("csharp")));

        using var receiver = new KernelCommandAndEventReceiver(_ => queue.Dequeue());

        queue.Should().NotBeEmpty();
    }

    [Fact]
    public async Task When_there_are_multiple_subscribers_there_are_no_concurrent_reads_from_underlying_source()
    {
        int readCount = 0;

        var enqueuedMessageCount = 3;
        for (var i = 0; i < enqueuedMessageCount; i++)
        {
            _messageQueue.Add(new CommandOrEvent(new SubmitCode(i.ToString())));
        }

        using var receiver = new KernelCommandAndEventReceiver(_ =>
        {
            var commandOrEvent = _messageQueue.Take();
            readCount++;
            return commandOrEvent;
        });

        var connectable = receiver.Publish();

        var subscriber1TaskCompletionSource = new TaskCompletionSource();
        var subscriber1Received = new List<CommandOrEvent>();
        using var subscriber1 = connectable.Subscribe(e =>
        {
            subscriber1Received.Add(e);
            if (subscriber1Received.Count == enqueuedMessageCount)
            {
                subscriber1TaskCompletionSource.SetResult();
            }
        });

        var subscriber2TasksCompletionSource = new TaskCompletionSource();
        var subscriber2Received = new List<CommandOrEvent>();
        using var subscriber2 = connectable.Subscribe(e =>
        {
            subscriber2Received.Add(e);
            if (subscriber2Received.Count == enqueuedMessageCount)
            {
                subscriber2TasksCompletionSource.SetResult();
            }
        });

        using var _ = connectable.Connect();

        Wait.Until(() => _messageQueue.Count == 0);

        // wait for both subscribers to receive all messages, but not longer than 5s
        await Task.WhenAny(
            Task.WhenAll(
                subscriber1TaskCompletionSource.Task,
                subscriber2TasksCompletionSource.Task
            ),
            Task.Delay(TimeSpan.FromSeconds(5))
        );

        subscriber1Received.Select(e => e.Command.As<SubmitCode>().Code).Should().BeEquivalentTo(new[] { "0", "1", "2" });

        subscriber2Received.Select(e => e.Command.As<SubmitCode>().Code).Should().BeEquivalentTo(new[] { "0", "1", "2" });

        readCount.Should().Be(enqueuedMessageCount);
    }

    [Fact]
    public async Task When_receiver_is_disposed_then_no_further_reads_occur()
    {
        int readCount = 0;

        for (int i = 0; i < 2; i++)
        {
            _messageQueue.Add(new CommandOrEvent(new SubmitCode(i.ToString())));
        }

        var receiver = new KernelCommandAndEventReceiver(t =>
        {
            readCount++;
            var commandOrEvent = _messageQueue.Take(t);
            return commandOrEvent;
        });

        using var subscription = receiver.Subscribe(e => { });

        Wait.Until(() => _messageQueue.Count == 0);

        var readCountAfterEmptied = readCount;

        receiver.Dispose();

        _messageQueue.Add(new CommandOrEvent(new SubmitCode("")));

        await Task.Delay(100);

        readCount.Should().Be(readCountAfterEmptied);
    }

    [Fact]
    public void New_subscriptions_can_be_made_after_all_subscribers_have_unsubscribed()
    {
        var count = 0;

        using var receiver = new KernelCommandAndEventReceiver(t =>
        {
            Thread.Sleep(50);

            var commandOrEvent = new CommandOrEvent(new SubmitCode($"{++count}"));

            return commandOrEvent;
        });

        var _ = receiver.Take(4).ToEnumerable().Count();

        var took = receiver.Take(4).ToEnumerable().Count();

        took.Should().Be(4);
    }

    [Fact]
    public async Task When_all_subscribers_are_unsubscribed_then_receiver_stop_reading()
    {
        var count = 0;

        using var receiver = new KernelCommandAndEventReceiver(t =>
        {
            Thread.Sleep(5);

            var commandOrEvent = new CommandOrEvent(new SubmitCode($"{count++}"));

            return commandOrEvent;
        });

        var t = receiver.Take(4).ToEnumerable().Count();

        await Task.Delay(50);

        var countAfterDispose = count;

        await Task.Delay(50);

        count.Should().Be(countAfterDispose);
    }
}