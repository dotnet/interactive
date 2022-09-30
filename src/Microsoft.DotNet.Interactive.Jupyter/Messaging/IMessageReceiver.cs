using System;

namespace Microsoft.DotNet.Interactive.Jupyter.Messaging
{
    public interface IMessageReceiver
    {
        IObservable<Message> Messages { get; }
    }
}
