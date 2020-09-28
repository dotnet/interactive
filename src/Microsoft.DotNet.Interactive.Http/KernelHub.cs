// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Http
{
    public class KernelHub : Hub
    {
        private readonly KernelHubConnection _connection;

        public KernelHub(KernelHubConnection connection, IHubContext<KernelHub> hubContext)
        {
            _connection = connection;
            _connection.RegisterContext(hubContext);
        }

        public async Task SubmitCommand(string kernelCommandEnvelope)
        {
            var envelope = KernelCommandEnvelope.Deserialize(kernelCommandEnvelope);
            var command = envelope.Command;
            await _connection.Kernel.SendAsync(command);
        }

        public async Task Connect()
        {
            await Clients.Caller.SendAsync("connected");
        }

    }
}