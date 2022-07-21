using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal class SubmitCodeHandler : CommandToJupyterMessageHandlerBase<SubmitCode>
    {
        public SubmitCodeHandler(IMessageSender sender, IMessageReceiver reciever) : base(sender, reciever)
        {
        }

        public override async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            var executeRequest = Messaging.Message.Create(new ExecuteRequest(command.Code));
            var executeReply = Receiver.Messages.ChildOf(executeRequest)
                                    .SelectContent()
                                    .Do(message => HandleReplyMessage(message, context))
                                    .OfType<Status>()
                                    .TakeUntil(m => m.ExecutionState == StatusValues.Idle); // run until the messages have been executed for this request

            await Sender.SendAsync(executeRequest);
            await executeReply.ToTask(context.CancellationToken);
        }

        private void HandleReplyMessage(Protocol.Message message, KernelInvocationContext context)
        {
            var command = context.Command;
            switch (message)
            {
                case (ExecuteReplyOk _):
                    context.Complete(command);
                    break;

                case (DisplayData displayData):
                    var formattedDisplayValues = displayData.Data.Select(d => new FormattedValue(d.Key, d.Value.ToString())).ToArray();

                    context.Publish(new DisplayedValueProduced(displayData.Data,
                                                            command,
                                                            formattedDisplayValues));
                    break;
                case (ExecuteResult result):
                    var formattedValues = result.Data.Select(d => new FormattedValue(d.Key, d.Value.ToString())).ToArray();

                    context.Publish(new ReturnValueProduced(result.Data,
                                                            command,
                                                            formattedValues));
                    break;
                case (Stream streamResult):
                    if (streamResult.Name == Stream.StandardOutput)
                    {
                        context.DisplayStandardOut(streamResult.Text);
                    }

                    if (streamResult.Name == Stream.StandardError)
                    {
                        context.DisplayStandardError(streamResult.Text);
                    }
                    break;
                case (Error error):
                    // how to translate traceback to exception;
                    context.Fail(command, message: error.EValue);
                    break;
                default:
                    break;
            }
        }
    }
}
