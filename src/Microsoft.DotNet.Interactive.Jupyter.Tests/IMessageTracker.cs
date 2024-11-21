// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public interface IMessageTracker : IMessageSender, IMessageReceiver, IDisposable
{
    public void Attach(IMessageSender sender, IMessageReceiver receiver);
    public IObservable<Message> SentMessages { get; }
    public IObservable<Message> ReceivedMessages { get; }
}