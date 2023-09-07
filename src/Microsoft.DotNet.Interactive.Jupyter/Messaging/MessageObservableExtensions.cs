// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;

namespace Microsoft.DotNet.Interactive.Jupyter.Messaging;

public static class MessageObservableExtensions
{
    public static IObservable<Message> ResponseOf(this IObservable<Message> observable, Message parentMessage)
    {
        return observable.Where(m => m?.ParentHeader?.MessageId == parentMessage.Header.MessageId);
    }

    public static IObservable<Protocol.Message> TakeUntilMessageType(this IObservable<Protocol.Message> observable, params string[] messageTypes)
    {
        return observable.TakeUntil(m => messageTypes.Contains(m.MessageType));
    }

    public static IObservable<Protocol.Message> Content(this IObservable<Message> observable)
    {
        return observable.Select(m => m.Content);
    }
}
