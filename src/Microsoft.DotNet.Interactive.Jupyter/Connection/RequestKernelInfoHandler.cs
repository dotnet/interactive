// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection;

internal class RequestKernelInfoHandler : CommandToJupyterMessageHandlerBase<RequestKernelInfo>
{
    public RequestKernelInfoHandler(IMessageSender sender, IMessageReceiver reciever) : base(sender, reciever)
    {
    }

    public override async Task HandleCommandAsync(RequestKernelInfo command, ICommandExecutionContext context, CancellationToken token)
    {
        // wait for kernel reply
        var request = Messaging.Message.Create(new KernelInfoRequest());
        var reply = Receiver.Messages.ChildOf(request)
                             .SelectContent()
                             .OfType<KernelInfoReply>()
                             .Take(1)
                             ;

        await Sender.SendAsync(request);
        var kernelInfoReply = await reply.ToTask(token);

        if (kernelInfoReply.Status != StatusValues.Ok)
        {
            // TODO: Need to split reply ok from error 
            context.Publish(new CommandFailed(null, command, "kernel returned failed"));
            return;
        }

        var kernelInfo = new KernelInfo(kernelInfoReply.Implementation,
                                        kernelInfoReply.LanguageInfo?.Name,
                                        kernelInfoReply.LanguageInfo?.Version);

        context.Publish(new KernelInfoProduced(kernelInfo, command));
    }
}
