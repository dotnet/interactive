// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Tests.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Directives;
using Message = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public interface IMessageTracker : IMessageSender, IMessageReceiver, IDisposable
{
    public void Attach(IMessageSender sender, IMessageReceiver receiver);
    public IObservable<Message> SentMessages { get; }
    public IObservable<Message> ReceivedMessages { get; }
}

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
        if (_testSender != null)
        {
            await _testSender.SendAsync(message);
        }

        _sentMessages.OnNext(message);
        return;
    }

    public void Dispose()
    {
        _sentMessages.Dispose();
        _receivedMessages.Dispose();
    }

    public IObservable<Message> SentMessages => _sentMessages;
    public IObservable<Message> ReceivedMessages => _receivedMessages;
}

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

public class TestJupyterKernelConnection : IJupyterKernelConnection
{
    private IJupyterKernelConnection _kernelConnection;
    private IMessageTracker _tracker;
    private bool _disposed = false;

    public TestJupyterKernelConnection(IMessageTracker messageTracker)
    {
        _tracker = messageTracker;
    }

    public void Attach(IJupyterKernelConnection kernelConnection)
    {
        if (kernelConnection == null)
        {
            throw new ArgumentNullException(nameof(kernelConnection));
        }

        _kernelConnection = kernelConnection;
        _tracker.Attach(kernelConnection.Sender, kernelConnection.Receiver);
    }
    public Uri Uri => _kernelConnection is null ? new Uri("test://") : _kernelConnection.Uri;

    public IMessageSender Sender => _tracker;

    public IMessageReceiver Receiver => _tracker;

    public void Dispose()
    {
        _kernelConnection?.Dispose();
        _tracker?.Dispose();
        _disposed = true;
    }

    public async Task StartAsync()
    {
        if (_kernelConnection != null)
        {
            await _kernelConnection.StartAsync();
        }
    }

    public bool IsDisposed => _disposed;
}

public class TestJupyterConnection : IJupyterConnection, IDisposable
{
    private IJupyterConnection _testJupyterConnection;
    private TestJupyterKernelConnection _testKernelConnection;
    private bool _disposed = false;
    private List<KernelSpec> _kernelSpecs = new();

    public TestJupyterConnection(TestJupyterKernelConnection testJupyterKernelConnection, List<KernelSpec> kernelSpecs = null)
    {
        _testKernelConnection = testJupyterKernelConnection;
        if (kernelSpecs != null)
        {
            _kernelSpecs = kernelSpecs;
        }
    }

    public void Attach(IJupyterConnection connection)
    {
        _testJupyterConnection = connection;
    }

    public async Task<IJupyterKernelConnection> CreateKernelConnectionAsync(string kernelSpecName)
    {
        if (_testJupyterConnection != null)
        {
            _testKernelConnection.Attach(await _testJupyterConnection.CreateKernelConnectionAsync(kernelSpecName));
        }

        return _testKernelConnection;
    }

    public TestJupyterKernelConnection KernelConnection => _testKernelConnection;

    public void Dispose()
    {
        (_testJupyterConnection as IDisposable)?.Dispose();
        _disposed = true;
    }

    public Task<IEnumerable<KernelSpec>> GetKernelSpecsAsync()
    {
        if (_testJupyterConnection == null)
        {
            return Task.FromResult(_kernelSpecs.AsEnumerable());
        }

        return _testJupyterConnection.GetKernelSpecsAsync();
    }

    public bool IsDisposed => _disposed;
}

public class TestJupyterConnectionOptions : IJupyterKernelConnectionOptions
{
    private IJupyterKernelConnectionOptions _testOptions;
    private TestJupyterConnection _connection;
    private SubscribedList<Message> _sentMessages;
    private SubscribedList<Message> _playbackMessages;
    private readonly bool _allowPlayback = false;

    public TestJupyterConnectionOptions(string kernelSpecName, string filePath, string fileName)
    {
        KernelSpecName = kernelSpecName;
        var fileToPlayback = GetFilePath(filePath, fileName);
        var json = File.ReadAllText(fileToPlayback);
        var messages = JsonSerializer.Deserialize<List<Message>>(json, MessageFormatter.SerializerOptions);
        Playback(messages);
    }

    public TestJupyterConnectionOptions(IReadOnlyCollection<Message> messagesToPlayback)
    {
        Playback(messagesToPlayback);
    }

    public TestJupyterConnectionOptions(IJupyterKernelConnectionOptions optionsToTest, string kernelSpecName, bool allowPlayback = false)
    {
        if (optionsToTest is null)
        {
            throw new ArgumentNullException(nameof(optionsToTest));
        }
        KernelSpecName = kernelSpecName;
        _allowPlayback = allowPlayback;
        Record(optionsToTest);
    }

    public TestJupyterConnectionOptions(TestJupyterConnection connection)
    {
        if (connection is null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        _connection = connection;
    }

    public void Record(IJupyterKernelConnectionOptions options)
    {
        _testOptions = options;
        MessageTracker = new MessageRecorder();
        _connection = new TestJupyterConnection(new TestJupyterKernelConnection(MessageTracker));
        if (_allowPlayback)
        {
            _sentMessages = MessageTracker.SentMessages.ToSubscribedList();
            _playbackMessages = MessageTracker.ReceivedMessages.ToSubscribedList();
        }
    }

    public void Playback(IReadOnlyCollection<Message> messages)
    {
        MessageTracker = new MessagePlayback(messages);
        _connection = new TestJupyterConnection(new TestJupyterKernelConnection(MessageTracker));
    }

    public IMessageTracker MessageTracker { get; private set; }

    public TestJupyterConnection Connection => _connection;

    public IJupyterConnection GetConnection(ConnectJupyterKernel connectCommand)
    {
        if (_testOptions is not null)
        {
            _connection.Attach(_testOptions.GetConnection(connectCommand));
        }

        return _connection;
    }

    public IReadOnlyCollection<KernelDirectiveParameter> GetParameters()
    {
        List<KernelDirectiveParameter> options = new();
        if (_testOptions is not null)
        {
            options.AddRange(_testOptions.GetParameters());
        }

        return options;
    }

    public void SaveState([CallerFilePath] string filePath = "", [CallerMemberName] string fileName = "")
    {
        if (_allowPlayback && _playbackMessages is not null && _sentMessages is not null)
        {
            var msgIds = _sentMessages.Select(m => m.Header.MessageId);

            string recordPath = GetFilePath(filePath, fileName);
            var messages = _playbackMessages.Where(m => msgIds.Contains(m.ParentHeader?.MessageId)).ToArray();
            var json = JsonSerializer.Serialize(messages, MessageFormatter.SerializerOptions);
            File.WriteAllText(recordPath, json);
        }
    }

    private string GetFilePath(string filePath, string fileName, string fileSuffix = null)
    {
        string file = $"{Path.GetFileNameWithoutExtension(filePath)}.{fileName}.{KernelSpecName + "." ?? ""}json";
        return Path.Combine(Path.GetDirectoryName(filePath), file);
    }

    public string KernelSpecName { get; set; }
}