// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Http;

internal class KernelCommandAndEventSignalRHubConnectionSender : IKernelCommandAndEventSender
{
    // QUESTION: (KernelCommandAndEventSignalRHubConnectionSender) tests?
    private readonly HubConnection _hubConnection;

    public KernelCommandAndEventSignalRHubConnectionSender(HubConnection sender)
    {
        _hubConnection = sender ?? throw new ArgumentNullException(nameof(sender));
        RemoteHostUri = KernelHost.CreateHostUri($"signalrhub{_hubConnection.ConnectionId}");
    }

    public async Task SendAsync(KernelCommand kernelCommand, CancellationToken cancellationToken)
    {
        await _hubConnection.SendAsync("kernelCommandFromServer", KernelCommandEnvelope.Serialize(KernelCommandEnvelope.Create(kernelCommand)),
            cancellationToken: cancellationToken);
    }

    public async Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken)
    {
        await _hubConnection.SendAsync("kernelEventFromServer", KernelEventEnvelope.Serialize(KernelEventEnvelope.Create(kernelEvent)), cancellationToken: cancellationToken);
    }

    public Uri RemoteHostUri { get; }
}