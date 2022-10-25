// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection;

internal class RequestSignatureHelpHandler : CommandToJupyterMessageHandlerBase<RequestSignatureHelp>
{
    public RequestSignatureHelpHandler(IMessageSender sender, IMessageReceiver reciever) : base(sender, reciever)
    {
    }

    public override async Task HandleCommandAsync(RequestSignatureHelp command, ICommandExecutionContext context, CancellationToken token)
    {
        var request = Messaging.Message.Create(new InspectRequest(
                                            code: command.Code,
                                            cursorPos: SourceUtilities.GetCursorOffsetFromPosition(command.Code, command.LinePosition), 
                                            detailLevel: 0));

        var reply = Receiver.Messages.FilterByParent(request)
                                .SelectContent()
                                .Do(replyMessage => HandleReplyMessage(replyMessage, command, context))
                                .TakeUntilMessageType(JupyterMessageContentTypes.InspectReply);
                                // run until we get a reply

        await Sender.SendAsync(request);
        await reply.ToTask(token);
    }

    private void HandleReplyMessage(Protocol.Message message, RequestSignatureHelp command, ICommandExecutionContext context)
    {
        switch (message)
        {
            case (InspectReply results):
                if (results.Status != StatusValues.Ok)
                {
                    // TODO: Need to split reply ok from error
                    context.Publish(new CommandFailed(null, command, "kernel returned failed"));
                    break;
                }

                if (results.Found)
                {
                    // the kernels return everything as a docstring. 
                    var signatureHelpItems = results.Data.Select(d => new SignatureInformation(string.Empty,
                                                                                               new FormattedValue(d.Key, d.Value.ToString()),
                                                                                               new List<ParameterInformation>())).ToArray();

                    context.Publish(new SignatureHelpProduced(command, signatureHelpItems,0, 0)); 
                }

                context.Publish(new CommandSucceeded(command));
                break;
            default:
                break;
        }
    }
}
