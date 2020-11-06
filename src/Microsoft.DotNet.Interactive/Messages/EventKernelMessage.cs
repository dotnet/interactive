using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Messages
{
    public class EventKernelMessage : KernelChannelMessage
    {
        public const string MessageLabel = "kernelEvents";

        public EventKernelMessage(
            KernelEvent kernelEvent)
            : base(MessageLabel)
        {
            Event = kernelEvent;
        }

        public KernelEvent Event { get; }
    }
}
