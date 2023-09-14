// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using ZeroMQMessage = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter;

public class IsCompleteRequestHandler : RequestHandlerBase<IsCompleteRequest>
{
    public IsCompleteRequestHandler(Kernel kernel, IScheduler scheduler = null)
        : base(kernel, scheduler ?? CurrentThreadScheduler.Instance)
    {
    }

    public async Task Handle(JupyterRequestContext context)
    {
        var isCompleteRequest = GetJupyterRequest(context);
        var targetKernelName = context.GetKernelName();
        var command = new RequestDiagnostics(isCompleteRequest.Code, targetKernelName);

        await SendAsync(context, command);
    }

    protected override void OnKernelEventReceived(
        KernelEvent @event,
        JupyterRequestContext context)
    {
        switch (@event)
        {
            case DiagnosticsProduced diagnosticsProduced:
                if (diagnosticsProduced.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                {
                    Reply(false, context.JupyterRequestMessageEnvelope, context.JupyterMessageSender);
                }
                break;

            case CommandSucceeded _:
                Reply(true, context.JupyterRequestMessageEnvelope, context.JupyterMessageSender);
                break;
        }
    }

    private void Reply(bool isComplete, ZeroMQMessage request, IJupyterMessageResponseSender jupyterMessageSender)
    {
        var status = isComplete ? "complete" : "incomplete";
        var indent = isComplete ? string.Empty : "*";
        // reply 
        var isCompleteReplyPayload = new IsCompleteReply(indent: indent, status: status);

        // send to server
        jupyterMessageSender.Send(isCompleteReplyPayload);
    }
}