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
using Pocket;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Jupyter.Tests.MessagePlayback>;
using Message = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

internal class MessagePlayback : IMessageTracker
{
    private readonly Subject<Message> _sentMessages = new();
    private readonly Subject<Message> _receivedMessages = new();
    private readonly ConcurrentQueue<Message> _processRequests = new();
    private readonly List<Message> _playbackMessages = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _requestProcessingLoopTask;

    public MessagePlayback(IReadOnlyCollection<Message> messages)
    {
        _playbackMessages.AddRange(messages);

        _requestProcessingLoopTask = Task.Factory.StartNew(
            RequestProcessingLoop);
    }

    public IObservable<Message> Messages => _receivedMessages;

    public Task SendAsync(Message message)
    {
        _sentMessages.OnNext(message);
        _processRequests.Enqueue(message);

        return Task.CompletedTask;
    }

    private async Task RequestProcessingLoop()
    {
        using var operation = Log.OnEnterAndConfirmOnExit();

        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                if (_processRequests.TryDequeue(out var message))
                {
                    // find appropriate message from playback to send back since we 
                    // can't match the messageIds.
                    var responses = _playbackMessages
                                    .GroupBy(m => new { MsgId = m.ParentHeader?.MessageId, MsgType = m.ParentHeader?.MessageType })
                                    .FirstOrDefault(g => g.Key.MsgType == message.Header.MessageType);

                    if (responses is not null)
                    {
                        foreach (var m in responses)
                        {
                            var replyMessage = new Message(
                                m.Header,
                                GetContent(message, m),
                                new Header(
                                    m.ParentHeader?.MessageType,
                                    message.Header.MessageId, // reply back with the sent message id
                                    m.ParentHeader.Version,
                                    m.ParentHeader.Session,
                                    m.ParentHeader.Username,
                                    m.ParentHeader.Date),
                                m.Signature, m.MetaData, m.Identifiers, m.Buffers, m.Channel);

                            _receivedMessages.OnNext(replyMessage);
                            _playbackMessages.Remove(m);
                        }
                    }
                }
                else
                {
                    try
                    {
                        await Task.Delay(50, _cancellationTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                operation.Fail(exception);
                return;
            }
        }

        operation.Succeed();
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
                return new CommClose(commId, commClose.Data);
            }
            else if (m.Content is CommMsg commMsg)
            {
                return new CommMsg(commId, commMsg.Data);
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
        try
        {
            _cancellationTokenSource.Cancel();
            _sentMessages.Dispose();
            _receivedMessages.Dispose();
            _requestProcessingLoopTask.Dispose();
        }
        catch (Exception exception)
        {
            Log.Error(exception);
        }
    }

    public IObservable<Message> SentMessages => _sentMessages;
    public IObservable<Message> ReceivedMessages => _receivedMessages;
}