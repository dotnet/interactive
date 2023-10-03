// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.Http;

internal class KernelHub : Hub
{
    private readonly KernelHubConnection _connection;

    public KernelHub(KernelHubConnection connection, IHubContext<KernelHub> hubContext)
    {
        _connection = connection;
        _connection.RegisterContext(hubContext);
    }

    public Task SubmitCommand(string kernelCommandEnvelope)
    {
        return KernelCommandFromRemote(kernelCommandEnvelope);
    }

    public async Task KernelCommandFromRemote(string kernelCommandEnvelope)
    {
        var envelope = KernelCommandEnvelope.Deserialize(kernelCommandEnvelope);
        var command = envelope.Command;
        await _connection.Kernel.SendAsync(command);
    }

    public Task KernelEvent(string kernelEventEnvelope)
    {
        return KernelEventFromRemote(kernelEventEnvelope);
    }

    public async Task KernelEventFromRemote(string kernelEventEnvelope)
    {
        var envelope = KernelEventEnvelope.Deserialize(kernelEventEnvelope);
        await _connection.HandleKernelEventFromClientAsync(envelope);
    }

    public async Task Connect()
    {
        await Clients.Caller.SendAsync("connected");
    }

}