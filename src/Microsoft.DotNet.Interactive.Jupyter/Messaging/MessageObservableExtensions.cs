using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;

namespace Microsoft.DotNet.Interactive.Jupyter.Messaging
{
    public static class MessageObservableExtensions
    {
        public static IObservable<Message> ChildOf(this IObservable<Message> observable, Message parentMessage)
        {
            var loopScheduler = new EventLoopScheduler();
            return observable.ObserveOn(loopScheduler)
                             .Where(m => m?.ParentHeader?.MessageId == parentMessage.Header.MessageId);
        }

        public static IObservable<Protocol.Message> SelectContent(this IObservable<Message> observable)
        {
            return observable.Select(m => m.Content);
        }
    }
}
