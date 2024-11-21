// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Jupyter.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public class TestJupyterKernelConnection : IJupyterKernelConnection
{
    private IJupyterKernelConnection _kernelConnection;
    private readonly IMessageTracker _tracker;
    private bool _disposed = false;

    public TestJupyterKernelConnection(IMessageTracker messageTracker)
    {
        _tracker = messageTracker;
    }

    public void Attach(IJupyterKernelConnection kernelConnection)
    {
        if (kernelConnection is null)
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
        if (_kernelConnection is not null)
        {
            await _kernelConnection.StartAsync();
        }
    }

    public bool IsDisposed => _disposed;
}