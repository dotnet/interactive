// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NetMQ;
using NetMQ.Sockets;
using Pocket;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class Heartbeat : IHostedService
    {
        private readonly string _address;
        private readonly ResponseSocket _server;

        public Heartbeat(ConnectionInformation connectionInformation)
        {
            if (connectionInformation == null)
            {
                throw new ArgumentNullException(nameof(connectionInformation));
            }

            _address = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.HBPort}";

            Logger<Heartbeat>.Log.Info($"using address {nameof(_address)}", _address);
            _server = new ResponseSocket();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _server.Bind(_address);
            Task.Run(() =>
            {
                using (Logger<Heartbeat>.Log.OnEnterAndExit())
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var data = _server.ReceiveFrameBytes();

                        // Echoing back whatever was received
                        _server.TrySendFrame(data);
                    }
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _server.Dispose();
            return Task.CompletedTask;
        }
    }
}