// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Messaging.Comms;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using System;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.CommandEvents;

internal class CommCommandEventAdapter : IDisposable,
    IKernelCommandHandler<SendValue>,
    IKernelCommandHandler<RequestValue>,
    IKernelCommandHandler<RequestValueInfos>
{
    private readonly CommAgent _agent;

    public CommCommandEventAdapter(CommAgent agent)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    public void Dispose()
    {
        _agent.Dispose();
    }

    public async Task HandleAsync(SendValue command, KernelInvocationContext context)
    {
        if (FailIfAgentIsClosed(command, context))
        {
            return;
        }
        await SendAsync(command, context.CancellationToken);
        return;
    }

    public async Task HandleAsync(RequestValue command, KernelInvocationContext context)
    {
        if (FailIfAgentIsClosed(command, context))
        {
            return;
        }

        return;
    }

    public async Task HandleAsync(RequestValueInfos command, KernelInvocationContext context)
    {
        if (FailIfAgentIsClosed(command, context))
        {
            return;
        }

        return;
    }

    private async Task<KernelEvent> SendAsync(KernelCommand command, CancellationToken token)
    {
        var responseObservable = GetResponseObservable();

        var request = new CommandEventCommPayload(command);

        await _agent.SendAsync(request.ToDictionary());
        var response = await responseObservable.ToTask(token);

        if (response is CommMsg commMsg)
        {
            var responseEvent = CommandEventCommPayload.FromDataDictionary(commMsg.Data);
            return responseEvent.EventEnvelope.Event;
        } 

        return new CommandFailed("Kernel didn't respond", command);
    }

    private IObservable<Protocol.Message> GetResponseObservable()
    {
        return _agent.Messages.TakeUntilMessageType(
            JupyterMessageContentTypes.CommMsg,
            JupyterMessageContentTypes.CommClose);
    }

    private bool FailIfAgentIsClosed(KernelCommand command, KernelInvocationContext context)
    {
        if (_agent.IsClosed)
        {
            context.Fail(command, null, "CommandEvent adapter channel closed with kernel shutdown");
            return true;
        }

        return false;
    }
}
