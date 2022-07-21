using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal abstract class CommandToJupyterMessageHandlerBase<TCommand> : IKernelCommandHandler<TCommand> where TCommand: KernelCommand
    {
        private readonly IMessageSender _sender;
        private readonly IMessageReceiver _receiver;

        public CommandToJupyterMessageHandlerBase(IMessageSender sender, IMessageReceiver reciever)
        {
            _receiver = reciever;
            _sender = sender;
        }

        protected IMessageReceiver Receiver => _receiver;

        protected IMessageSender Sender => _sender;

        public abstract Task HandleAsync(TCommand command, KernelInvocationContext context);
    }
}
