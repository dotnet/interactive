using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Messages
{
    public class CommandKernelMessage : KernelChannelMessage
    {
        public const string MessageLabel = "kernelCommands";

        public CommandKernelMessage(KernelCommand command)
            : base(MessageLabel)
        {
            Command = command;
        }

        public KernelCommand Command { get; }

        public override object PayloadForSerializationModel() => KernelCommandEnvelope.SerializeToModel(Command);
    }
}
