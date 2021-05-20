// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Http
{
    internal class SignalROutputTextStream : OutputTextStream
    {
        private readonly HubConnection _connection;

        public SignalROutputTextStream(HubConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
        protected override void WriteText(string text)
        {
            _connection.SendAsync("submitCommand", text);
        }
    }

    public class KernelCommandAndEventSignalRSender : IKernelCommandAndEventSender
    {
        private readonly IHubContext<KernelHub> _sender;


        public KernelCommandAndEventSignalRSender(IHubContext<KernelHub> sender)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }

        public async Task SendAsync(KernelCommand kernelCommand, CancellationToken cancellationToken)
        {
            await _sender.Clients.All.SendAsync("commandFromServer", KernelCommandEnvelope.Serialize(KernelCommandEnvelope.Create(kernelCommand)), cancellationToken: cancellationToken);

        }

        public async Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken)
        {
            await _sender.Clients.All.SendAsync("eventFromServer", KernelEventEnvelope.Serialize(KernelEventEnvelope.Create(kernelEvent)), cancellationToken: cancellationToken);

        }
    }
}