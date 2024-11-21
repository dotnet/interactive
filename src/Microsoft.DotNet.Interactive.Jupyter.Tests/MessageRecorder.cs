// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

internal class MessageRecorder : IMessageTracker
{
    private IMessageSender _testSender;
    private IMessageReceiver _testReceiver;

    private readonly Subject<Message> _sentMessages = new();
    private readonly Subject<Message> _receivedMessages = new();

    public IObservable<Message> Messages => _testReceiver != null ? _testReceiver.Messages : throw new InvalidOperationException("No connection");

    public void Attach(IMessageSender sender, IMessageReceiver receiver)
    {
        _testSender = sender;
        _testReceiver = receiver;

        _testReceiver.Messages.Subscribe(_receivedMessages);
    }

    public async Task SendAsync(Message message)
    {
        if (_testSender is not null)
        {
            await _testSender.SendAsync(message);
        }

        _sentMessages.OnNext(message);
    }

    public void Dispose()
    {
        _sentMessages.Dispose();
        _receivedMessages.Dispose();
    }

    public IObservable<Message> SentMessages => _sentMessages;
    public IObservable<Message> ReceivedMessages => _receivedMessages;
}