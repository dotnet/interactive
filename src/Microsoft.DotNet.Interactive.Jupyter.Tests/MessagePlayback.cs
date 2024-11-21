// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Message = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

internal class MessagePlayback : IMessageTracker
{
    private readonly Subject<Message> _sentMessages = new();
    private readonly Subject<Message> _receivedMessages = new();
    private readonly ConcurrentQueue<Message> _processRequests = new();
    private readonly List<Message> _playbackMessages = new();
    private readonly CancellationTokenSource _cts = new();
    public IObservable<Message> Messages => _receivedMessages;

    public MessagePlayback(IReadOnlyCollection<Message> messages)
    {
        _playbackMessages.AddRange(messages);

        // FIX: (MessagePlayback) make sure this is awaited
        var requestProcessing = Task.Run(() => ProcessRequestsAsync());
    }

    public Task SendAsync(Message message)
    {
        _sentMessages.OnNext(message);
        _processRequests.Enqueue(message);

        return Task.CompletedTask;
    }

    private async Task ProcessRequestsAsync()
    {
        await Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                if (_processRequests.TryDequeue(out var message))
                {
                    // find appropriate message from playback to send back since we 
                    // can't match the messageIds.
                    var responses = _playbackMessages
                                    .GroupBy(m => new { MsgId = m.ParentHeader?.MessageId, MsgType = m.ParentHeader?.MessageType })
                                    .FirstOrDefault(g => g.Key.MsgType == message.Header.MessageType);


                    foreach (var m in responses)
                    {
                        var replyMessage = new Message(
                            m.Header,
                            GetContent(message, m),
                            new Header(
                                m.ParentHeader?.MessageType,
                                message.Header.MessageId,  // reply back with the sent message id
                                m.ParentHeader.Version,
                                m.ParentHeader.Session,
                                m.ParentHeader.Username,
                                m.ParentHeader.Date),
                            m.Signature, m.MetaData, m.Identifiers, m.Buffers, m.Channel);

                        _receivedMessages.OnNext(replyMessage);
                        _playbackMessages.Remove(m);
                    }
                }
                else
                {
                    await Task.Delay(50);
                }
            }
        }, _cts.Token);
    }

    private Protocol.Message GetContent(Message message, Message m)
    {
        string commId = null;
        if (message.Content is CommOpen commOpen)
        {
            commId = commOpen.CommId;
        }
        else if (message.Content is CommMsg commMsg)
        {
            commId = commMsg.CommId;
        }

        if (commId is not null)
        {
            if (m.Content is CommClose commClose)
            {
                return new CommClose(commId, commClose.Data as IReadOnlyDictionary<string, object>);
            }
            else if (m.Content is CommMsg commMsg)
            {
                return new CommMsg(commId, commMsg.Data as IReadOnlyDictionary<string, object>);
            }
        }

        return m.Content;
    }

    public void Attach(IMessageSender sender, IMessageReceiver receiver)
    {
        // No-op;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _sentMessages.Dispose();
        _receivedMessages.Dispose();
    }

    public IObservable<Message> SentMessages => _sentMessages;
    public IObservable<Message> ReceivedMessages => _receivedMessages;
}