// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Linq;
using Message = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;


internal interface IMessageTracker : IMessageSender, IMessageReceiver
{
    public void Attach(IMessageSender sender, IMessageReceiver receiver);
    public IObservable<Message> SentMessages { get; }
    public IObservable<Message> ReceivedMessages { get; }
}

internal class MessageRecorder : IMessageSender, IMessageReceiver, IMessageTracker
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

    public IObservable<Message>  SentMessages => _sentMessages;
    public IObservable<Message> ReceivedMessages => _receivedMessages;
}

internal class MessagePlayback : IMessageTracker
{
    private readonly Subject<Message> _sentMessages = new();
    private readonly Subject<Message> _receivedMessages = new();
    
    private readonly List<Message> _playbackMessages = new();

    public IObservable<Message> Messages => _receivedMessages;

    public MessagePlayback(List<Message> messages)
    {
        _playbackMessages.AddRange(messages);
    }

    public Task SendAsync(Message message)
    {
        _sentMessages.OnNext(message);

        // find the corresponding message in the recieved list and playback
        var responses = _playbackMessages.Where(m => m.ParentHeader.MessageId == message.Header.MessageId);
        foreach (var m in responses)
        {
            _receivedMessages.OnNext(m);
        }

        return Task.CompletedTask;
    }

    public void Attach(IMessageSender sender, IMessageReceiver receiver)
    {
        // No-op;
    }

    public IObservable<Message> SentMessages => _sentMessages;
    public IObservable<Message> ReceivedMessages => _receivedMessages;
}

internal class TestJupyterKernelConnection : IJupyterKernelConnection
{
    private IJupyterKernelConnection _kernelConnection;
    private IMessageTracker _tracker;

    public TestJupyterKernelConnection(IMessageTracker messageTracker)
    {
        _tracker = messageTracker;
    }

    public void Attach(IJupyterKernelConnection kernelConnection)
    {
        _kernelConnection = kernelConnection;
        _tracker.Attach(kernelConnection.Sender, kernelConnection.Receiver);
    }
    public Uri Uri => _kernelConnection.Uri;

    public IMessageSender Sender => _tracker;

    public IMessageReceiver Receiver => _tracker;

    public void Dispose()
    {
        _kernelConnection.Dispose();
    }

    public async Task StartAsync()
    {
        await _kernelConnection.StartAsync();
    }
}

internal class TestJupyterConnection : IJupyterConnection
{
    private IJupyterConnection _testJupyterConnection;
    private TestJupyterKernelConnection _testKernelConnection;

    public TestJupyterConnection(TestJupyterKernelConnection testJupyterKernelConnection)
    {
        _testKernelConnection = testJupyterKernelConnection;
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

    public void Dispose()
    {
        _testJupyterConnection.Dispose();
    }

    public Task<IEnumerable<KernelSpec>> GetKernelSpecsAsync()
    {
        return _testJupyterConnection.GetKernelSpecsAsync();
    }
}

internal class TestJupyterConnectionOptions : IJupyterKernelConnectionOptions
{
    private IJupyterKernelConnectionOptions _testOptions;
    private TestJupyterConnection _connection;
    
    public TestJupyterConnectionOptions(IJupyterKernelConnectionOptions optionsToTest = null)
    {
        if (optionsToTest != null)
        {
            Record(optionsToTest);
        }
    }
    
    public void Record(IJupyterKernelConnectionOptions options)
    {
        _testOptions = options;
        MessageTracker = new MessageRecorder();
        _connection = new TestJupyterConnection(new TestJupyterKernelConnection(MessageTracker));
    }

    public void Playback(List<Message> messages)
    {
        MessageTracker = new MessagePlayback(messages);
        _connection = new TestJupyterConnection(new TestJupyterKernelConnection(MessageTracker));
    }

    public IMessageTracker MessageTracker { get; private set; }
    public TestJupyterConnection Connection => _connection;

    public IJupyterConnection GetConnection(ParseResult connectionOptionsParseResult)
    {
        if (_testOptions != null)
        {
            _connection.Attach(_testOptions.GetConnection(connectionOptionsParseResult));
        }

        return _connection;
    }

    public IReadOnlyCollection<Option> GetOptions()
    {
        List<Option> options = new();
        if (_testOptions != null)
        {
            options.AddRange(_testOptions.GetOptions());
        }

        return options;
    }
}