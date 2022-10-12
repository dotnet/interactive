﻿using Microsoft.DotNet.Interactive.Commands;
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

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
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

            var reply = Receiver.Messages.ChildOf(request)
                                    .SelectContent()
                                    .Do(replyMessage => HandleReplyMessage(replyMessage, command, context))
                                    .TakeUntilMessageType(JupyterMessageContentTypes.InspectReply, JupyterMessageContentTypes.Error);
                                    // run until we get a definitive pass or fail

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
                        // TODO: Add an error trace
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
                case (Error error):
                    // TODO: how to translate traceback to exception;
                    context.Publish(new CommandFailed(null, command, error.EValue));
                    break;
                default:
                    break;
            }
        }
    }
}