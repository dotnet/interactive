// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection;

internal class RequestHoverTextHandler : CommandToJupyterMessageHandlerBase<RequestHoverText>
{
    public RequestHoverTextHandler(IMessageSender sender, IMessageReceiver reciever) : base(sender, reciever)
    {
    }

    public override async Task HandleCommandAsync(RequestHoverText command, ICommandExecutionContext context, CancellationToken token)
    {
        var request = Messaging.Message.Create(new InspectRequest(
                                            code: command.Code,
                                            cursorPos: SourceUtilities.GetCursorOffsetFromPosition(command.Code, command.LinePosition), 
                                            detailLevel: 0));

        var reply = Receiver.Messages.ChildOf(request)
                                .SelectContent()
                                .Do(replyMessage => HandleReplyMessage(replyMessage, command, context))
                                .TakeUntilMessageType(JupyterMessageContentTypes.InspectReply, JupyterMessageContentTypes.Error);
                                // run until we get a definitive pass or fail

        await Sender.SendAsync(request);
        await reply.ToTask(token);
    }

    private void HandleReplyMessage(Protocol.Message message, RequestHoverText command, ICommandExecutionContext context)
    {
        switch (message)
        {
            case (InspectReply results):
                if (results.Status != StatusValues.Ok)
                {
                    // TODO: Add an error trace
                    context.Publish(new CommandFailed(null, command, "kernel returned failed"));
                    break;
                }

                if (results.Found)
                {
                    // TODO: The data returned is using ANSI encoding. Need to format to show correctly in hover text
                    var content = results.Data.Select(d => new FormattedValue(d.Key, d.Value.ToString())).ToArray();
                    context.Publish(new HoverTextProduced(command, content, new LinePositionSpan(command.LinePosition, command.LinePosition))); // we don't get any line position back from kernel
                }

                context.Publish(new CommandSucceeded(command));
                break;
            case (Error error):
                // TODO: how to translate traceback to exception;
                context.Publish(new CommandFailed(null, command, error.EValue));
                break;
            default:
                break;
        }
    }
}
