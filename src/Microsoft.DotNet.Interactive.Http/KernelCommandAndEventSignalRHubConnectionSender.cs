// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Http
{
    public class KernelCommandAndEventSignalRHubConnectionSender : IKernelCommandAndEventSender
    {
        // QUESTION: (KernelCommandAndEventSignalRHubConnectionSender) tests?
        private readonly HubConnection  _sender;

        public KernelCommandAndEventSignalRHubConnectionSender(HubConnection sender)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }

        public async Task SendAsync(KernelCommand kernelCommand, CancellationToken cancellationToken)
        {
            // FIX: remove this one as this is for backward compatibility
            await _sender.SendAsync("submitCommand", KernelCommandEnvelope.Serialize(KernelCommandEnvelope.Create(kernelCommand)), cancellationToken: cancellationToken);

            await _sender.SendAsync("kernelCommandFromServer", KernelCommandEnvelope.Serialize(KernelCommandEnvelope.Create(kernelCommand)), cancellationToken: cancellationToken);

        }

        public async Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken)
        {
            await _sender.SendAsync("kernelEventFromServer", KernelEventEnvelope.Serialize(KernelEventEnvelope.Create(kernelEvent)), cancellationToken: cancellationToken);

        }
    }
}