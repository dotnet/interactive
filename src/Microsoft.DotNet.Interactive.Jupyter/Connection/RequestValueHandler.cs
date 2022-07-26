using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal class RequestValueHandler : CommandToJupyterMessageHandlerBase<RequestValue>
    {
        public RequestValueHandler(IMessageSender sender, IMessageReceiver reciever) : base(sender, reciever)
        {
        }


        public override async Task HandleCommandAsync(RequestValue command, ICommandExecutionContext context, CancellationToken token)
        {
            var inspectRequest = Messaging.Message.Create(new InspectRequest(command.Name, 0, 0));
            var inspectReply = Receiver.Messages.ChildOf(inspectRequest)
                                    .SelectContent()
                                    .Do(replyMessage => HandleReplyMessage(replyMessage, command, context))
                                    .TakeUntilMessageType(JupyterMessageContentTypes.InspectReply, JupyterMessageContentTypes.Error);
            // run until we get a definitive pass or fail

            await Sender.SendAsync(inspectRequest);
            await inspectReply.ToTask(token);
        }

        private void HandleReplyMessage(Protocol.Message message, RequestValue command, ICommandExecutionContext context)
        {
            switch (message)
            {
                case (InspectReply inspectReply):
                    if (inspectReply.Status == StatusValues.Ok && TryParseValue(command, inspectReply, out ValueProduced valueProduced))
                    {
                        context.Publish(valueProduced);
                        context.Publish(new CommandSucceeded(command));
                        break;
                    }

                    // TODO: Add a InspectReplyError object
                    context.Publish(new CommandFailed(null, command, $"Failed to retrieve value for '{command.Name}' from {command.TargetKernelName}"));
                    break;
                case (Error error):
                    // TODO: how to translate traceback to exception;
                    context.Publish(new CommandFailed(null, command, error.EValue));
                    break;
                default:
                    break;
            }
        }

        private bool TryParseValue(RequestValue originalCommand, InspectReply inspectReply, out ValueProduced valueProduced)
        {
            valueProduced = new ValueProduced("testValue", originalCommand.Name, new FormattedValue("text/plain", "testtest"), originalCommand);
            return true;
        }
    }
}
