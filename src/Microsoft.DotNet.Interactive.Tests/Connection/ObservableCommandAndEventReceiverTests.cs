// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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

        using var receiver = new CommandAndEventReciever(_ => queue.Dequeue());

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

        using var receiver = new CommandAndEventReciever(_ =>
        {
            var commandOrEvent = _messageQueue.Take();
            readCount++;
            return commandOrEvent;
        });

        var connectable = receiver.Publish();

        var subscriber1Received = new List<CommandOrEvent>();
        using var subscriber1 = connectable.Subscribe(e => subscriber1Received.Add(e));

        var subscriber2Received = new List<CommandOrEvent>();
        using var subscriber2 = connectable.Subscribe(e => subscriber2Received.Add(e));

        using var _ = connectable.Connect();

        Wait.Until(() => _messageQueue.Count == 0);

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
            _messageQueue.Add(new CommandOrEvent(new SubmitCode("")));
        }

        using var receiver = new CommandAndEventReciever(t =>
        {
            var commandOrEvent = _messageQueue.Take(t);
            readCount++;
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
}