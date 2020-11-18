using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

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

        public override object PayloadForSerializationModel() => KernelEventEnvelope.SerializeToModel(Event);
    }
}
