using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Messaging
{
    internal interface IMessageReceiver
    {
        IObservable<Message> Messages { get; }
    }
}
