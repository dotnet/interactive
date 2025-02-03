// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NetMQ;
using NetMQ.Sockets;
using Pocket;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Jupyter.Heartbeat>;

namespace Microsoft.DotNet.Interactive.Jupyter;

public class Heartbeat : IHostedService
{
    private readonly string _address;
    private readonly ResponseSocket _server;
    private CancellationToken _cancellationToken;
    private Task _startReceiveLoop;

    public Heartbeat(ConnectionInformation connectionInformation)
    {
        if (connectionInformation is null)
        {
            throw new ArgumentNullException(nameof(connectionInformation));
        }

        _address = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.HBPort}";

        Log.Info($"using address {nameof(_address)}", _address);
        _server = new ResponseSocket();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;

        _server.Bind(_address);

        _startReceiveLoop = Task.Factory.StartNew(ReceiveLoop, creationOptions: TaskCreationOptions.LongRunning);

        return Task.CompletedTask;
    }

    private void ReceiveLoop()
    {
        using var _ = Log.OnEnterAndExit();

        while (!_cancellationToken.IsCancellationRequested)
        {
            var data = _server.ReceiveFrameBytes();

            // Echoing back whatever was received
            _server.TrySendFrame(data);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _server.Dispose();
        return Task.CompletedTask;
    }
}