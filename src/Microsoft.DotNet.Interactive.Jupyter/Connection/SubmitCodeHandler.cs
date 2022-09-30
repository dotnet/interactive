using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal class SubmitCodeHandler : CommandToJupyterMessageHandlerBase<SubmitCode>
    {
        public SubmitCodeHandler(IMessageSender sender, IMessageReceiver reciever) : base(sender, reciever)
        {
        }

        public override async Task HandleCommandAsync(SubmitCode command, ICommandExecutionContext context, CancellationToken token)
        {
            var executeRequest = Messaging.Message.Create(new ExecuteRequest(command.Code.NormalizeLineEndings()));
            var executeReply = Receiver.Messages.ChildOf(executeRequest)
                                    .SelectContent()
                                    .Do(replyMessage => HandleReplyMessage(replyMessage, command, context))
                                    .TakeUntil(m => m.MessageType == JupyterMessageContentTypes.Error
                                    || (m.MessageType == JupyterMessageContentTypes.Status && (m as Status)?.ExecutionState == StatusValues.Idle));
                                    //.TakeUntilMessageType(JupyterMessageContentTypes.ExecuteReply, JupyterMessageContentTypes.Error); 
                                    // run until we get a definitive pass or fail

            await Sender.SendAsync(executeRequest);
            await executeReply.ToTask(token);
        }

        private void HandleReplyMessage(Protocol.Message message, SubmitCode command, ICommandExecutionContext context)
        {
            switch (message)
            {
                case (Status status):
                    if (status.ExecutionState == StatusValues.Idle)
                    {
                        // Since Error will trigger CommandFailed event before Status.Idle is reported, assume that 
                        // status idle means command is successful.
                        context.Publish(new CommandSucceeded(command));
                    }
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
                        context.Publish(
                            new StandardOutputValueProduced(
                                command,
                                new[] { new FormattedValue(PlainTextFormatter.MimeType, streamResult.Text) }));
                    }

                    if (streamResult.Name == Stream.StandardError)
                    {
                        context.Publish(
                            new StandardErrorValueProduced(
                                command,
                                new[] { new FormattedValue(PlainTextFormatter.MimeType, streamResult.Text) }));
                    }
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
