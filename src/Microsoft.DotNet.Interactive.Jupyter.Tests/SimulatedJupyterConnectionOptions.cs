// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Tests.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Directives;
using Message = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public sealed class SimulatedJupyterConnectionOptions : IJupyterKernelConnectionOptions
{
    private IJupyterKernelConnectionOptions _testParameters;
    private SubscribedList<Message> _sentMessages;
    private SubscribedList<Message> _playbackMessages;
    private readonly bool _allowPlayback = false;

    public SimulatedJupyterConnectionOptions(string kernelSpecName, string filePath, string fileName)
    {
        KernelSpecName = kernelSpecName;
        var fileToPlayback = GetFilePath(filePath, fileName);
        var json = File.ReadAllText(fileToPlayback);
        var messages = JsonSerializer.Deserialize<List<Message>>(json, MessageFormatter.SerializerOptions);
        Playback(messages);
    }

    public SimulatedJupyterConnectionOptions(IReadOnlyCollection<Message> messagesToPlayback)
    {
        Playback(messagesToPlayback);
    }

    public SimulatedJupyterConnectionOptions(
        IJupyterKernelConnectionOptions optionsToTest, 
        string kernelSpecName, 
        bool allowPlayback = false)
    {
        if (optionsToTest is null)
        {
            throw new ArgumentNullException(nameof(optionsToTest));
        }
        KernelSpecName = kernelSpecName;
        _allowPlayback = allowPlayback;
        Record(optionsToTest);
    }

    public SimulatedJupyterConnectionOptions(TestJupyterConnection connection)
    {
        if (connection is null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        Connection = connection;
    }

    public void Record(IJupyterKernelConnectionOptions options)
    {
        _testParameters = options;
        MessageTracker = new MessageRecorder();
        Connection = new TestJupyterConnection(new TestJupyterKernelConnection(MessageTracker));
        if (_allowPlayback)
        {
            _sentMessages = MessageTracker.SentMessages.ToSubscribedList();
            _playbackMessages = MessageTracker.ReceivedMessages.ToSubscribedList();
        }
    }

    public void Playback(IReadOnlyCollection<Message> messages)
    {
        MessageTracker = new MessagePlayback(messages);
        Connection = new TestJupyterConnection(new TestJupyterKernelConnection(MessageTracker));
    }

    public IMessageTracker MessageTracker { get; private set; }

    public TestJupyterConnection Connection { get; private set; }

    public IJupyterConnection GetConnection(ConnectJupyterKernel connectCommand)
    {
        if (_testParameters is not null)
        {
            Connection.Attach(_testParameters.GetConnection(connectCommand));
        }

        return Connection;
    }

    public IReadOnlyCollection<KernelDirectiveParameter> GetParameters()
    {
        List<KernelDirectiveParameter> parameters = new();

        if (_testParameters is not null)
        {
            parameters.AddRange(_testParameters.GetParameters());
        }

        return parameters;
    }

    public void SaveState(
        [CallerFilePath] string filePath = "", 
        [CallerMemberName] string fileName = "")
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

    private string GetFilePath(string filePath, string fileName)
    {
        string file = $"{Path.GetFileNameWithoutExtension(filePath)}.{fileName}.{KernelSpecName + "." ?? ""}json";
        return Path.Combine(Path.GetDirectoryName(filePath), file);
    }

    public string KernelSpecName { get; set; }
}