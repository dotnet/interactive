using Microsoft.DotNet.Interactive.Commands;

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
    }
}
