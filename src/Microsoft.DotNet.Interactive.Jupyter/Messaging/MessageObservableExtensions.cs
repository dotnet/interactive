﻿using System;
using System.Linq;
using System.Reactive.Linq;

namespace Microsoft.DotNet.Interactive.Jupyter.Messaging
{
    public static class MessageObservableExtensions
    {
        public static IObservable<Message> ChildOf(this IObservable<Message> observable, Message parentMessage)
        {
            return observable.Where(m => m?.ParentHeader?.MessageId == parentMessage.Header.MessageId);
        }
        public static IObservable<Protocol.Message> TakeUntilMessageType(this IObservable<Protocol.Message> observable, params string[] messageTypes)
        {
            return observable.TakeUntil(m => messageTypes.Contains(m.MessageType));
        }

        public static IObservable<Protocol.Message> SelectContent(this IObservable<Message> observable)
        {
            return observable.Select(m => m.Content);
        }

        public static IObservable<Protocol.Message> FilterByMessageType(this IObservable<Protocol.Message> observable, params string[] messageTypes)
        {
            return observable.Where(m => messageTypes.Contains(m.MessageType));
        }
    }
}