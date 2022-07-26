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
    internal class RequestValueInfoHandler : CommandToJupyterMessageHandlerBase<RequestValueInfos>
    {
        public RequestValueInfoHandler(IMessageSender sender, IMessageReceiver reciever) : base(sender, reciever)
        {
        }

        public override async Task HandleCommandAsync(RequestValueInfos command, ICommandExecutionContext context, CancellationToken token)
        {
            var variableNamesQuery = "_rwho_ls = %who_ls\nprint(_rwho_ls)";
            var request = Messaging.Message.Create(new ExecuteRequest(variableNamesQuery, storeHistory: false));
            var reply = Receiver.Messages.ChildOf(request)
                                    .SelectContent()
                                    .Do(replyMessage => HandleReplyMessage(replyMessage, command, context))
                                    .TakeUntilMessageType(JupyterMessageContentTypes.InspectReply, JupyterMessageContentTypes.Error); 
                                    // run until we get a definitive pass or fail

            await Sender.SendAsync(request);
            await reply.ToTask(token);
        }

        private void HandleReplyMessage(Protocol.Message message, RequestValueInfos command, ICommandExecutionContext context)
        {
            switch (message)
            {
                case (ExecuteReplyOk _):
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
