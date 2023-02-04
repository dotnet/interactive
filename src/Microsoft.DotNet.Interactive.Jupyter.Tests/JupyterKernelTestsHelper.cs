// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Message = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

internal sealed class JupyterZMQConnectionHelper 
{
    public const string TEST_DOTNET_JUPYTER_ZMQ_CONN = nameof(TEST_DOTNET_JUPYTER_ZMQ_CONN);
    public static readonly string SkipReason;
    
    static JupyterZMQConnectionHelper()
    {
        SkipReason = TestConnectionAndReturnSkipReason();
    }

    internal static string TestConnectionAndReturnSkipReason()
    {
        string connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return $"Environment variable {TEST_DOTNET_JUPYTER_ZMQ_CONN} is not set. To run tests that require "
                   + "Jupyter server running, this environment variable must be set";
        }

        return null;
    }

    public static string GetConnectionString()
    {
        return Environment.GetEnvironmentVariable(TEST_DOTNET_JUPYTER_ZMQ_CONN);
    }
}

internal sealed class JupyterHttpConnectionHelper
{
    public const string TEST_DOTNET_JUPYTER_HTTP_CONN = nameof(TEST_DOTNET_JUPYTER_HTTP_CONN);
    public static readonly string SkipReason;

    static JupyterHttpConnectionHelper()
    {
        SkipReason = TestConnectionAndReturnSkipReason();
    }

    internal static string TestConnectionAndReturnSkipReason()
    {
        string connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return $"Environment variable {TEST_DOTNET_JUPYTER_HTTP_CONN} is not set. To run tests that require "
                   + "Jupyter server running, this environment variable must be set to a valid connection string value with --url and --token.";
        }

        return null;
    }

    public static string GetConnectionString()
    {
        // e.g. --url <server> --token <token>
        return Environment.GetEnvironmentVariable(TEST_DOTNET_JUPYTER_HTTP_CONN);
    }
}


internal class JupyterKernelTestHelper
{
    public static TestJupyterConnectionOptions GetConnectionOptions(Type connectionOptionsToTest = null)
    {
        var options = new TestJupyterConnectionOptions();
        if (connectionOptionsToTest == typeof(JupyterHttpKernelConnectionOptions) && JupyterHttpConnectionHelper.SkipReason is null)
        {
            options.Record(new JupyterHttpKernelConnectionOptions(), JupyterHttpConnectionHelper.GetConnectionString());
        }
        else if (connectionOptionsToTest == typeof(JupyterLocalKernelConnectionOptions) && JupyterZMQConnectionHelper.SkipReason is null)
        {
            options.Record(new JupyterLocalKernelConnectionOptions());
        }
        else if (connectionOptionsToTest == null)
        {
            options.Playback(null);
        }

        return options;
    }
}

internal interface IMessageTracker : IMessageSender, IMessageReceiver
{
    public void Attach(IMessageSender sender, IMessageReceiver receiver);
    public IReadOnlyCollection<Message> SentMessages { get; }
    public IReadOnlyCollection<Message> ReceivedMessages { get; }
}

internal class MessageRecorder : IMessageSender, IMessageReceiver, IMessageTracker
{
    private IMessageSender _testSender;
    private IMessageReceiver _testReceiver;
    
    private readonly List<Message> _sentMessages = new();
    private readonly List<Message> _receivedMessages = new();

    private readonly Dictionary<string, string> _ids = new();

    public IObservable<Message> Messages => _testReceiver != null ? _testReceiver.Messages : throw new InvalidOperationException("No connection");

    public void Attach(IMessageSender sender, IMessageReceiver receiver)
    {
        _testSender = sender;
        _testReceiver = receiver;

        _testReceiver.Messages.Subscribe(m => _receivedMessages.Add(m));
    }

    public async Task SendAsync(Message message)
    {
        if (_testSender != null)
        {
            await _testSender.SendAsync(message);
        }

        _sentMessages.Add(message);
        return ;
    }

    public IReadOnlyCollection<Message> SentMessages => _sentMessages;
    public IReadOnlyCollection<Message> ReceivedMessages => _receivedMessages;
}

internal class MessagePlayback : IMessageTracker
{
    private readonly List<Message> _sentMessages = new();
    private readonly List<Message> _receivedMessages = new();
    private readonly Subject<Message> _playbackMessages = new();

    public IObservable<Message> Messages => _playbackMessages;

    public MessagePlayback(List<Message> messages)
    {
        _receivedMessages.AddRange(messages);
    }

    public Task SendAsync(Message message)
    {
        _sentMessages.Add(message);

        // find the corresponding message in the recieved list and playback
        var responses = _receivedMessages.Where(m => m.ParentHeader.MessageId == message.Header.MessageId);
        foreach (var m in responses)
        {
            _playbackMessages.OnNext(m);
        }

        return Task.CompletedTask;
    }

    public void Attach(IMessageSender sender, IMessageReceiver receiver)
    {
        // No-op;
    }

    public IReadOnlyCollection<Message> SentMessages => _sentMessages;
    public IReadOnlyCollection<Message> ReceivedMessages => _receivedMessages;
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
    private string _testConnectionString = "--test";
    private TestJupyterConnection _connection;

    public void Record(IJupyterKernelConnectionOptions options, string connectionString = "")
    {
        _testOptions = options;
        _testConnectionString = $"{_testConnectionString} {connectionString}";

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
    public string TestConnectionString => _testConnectionString;
    
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
        options.Add(new Option<bool>("--test"));
        if (_testOptions != null)
        {

            options.AddRange(_testOptions.GetOptions());
        }

        return options;
    }
}