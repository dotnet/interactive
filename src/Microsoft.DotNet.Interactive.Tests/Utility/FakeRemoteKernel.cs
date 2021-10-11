// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Tests.Parsing;

namespace Microsoft.DotNet.Interactive.Tests.Utility
{
    internal class FakeRemoteKernel : FakeKernel
    {
        public IKernelCommandAndEventSender Sender { get; }
        public IKernelCommandAndEventReceiver Receiver { get; }

        public FakeRemoteKernel([CallerMemberName] string name = null) : base(name)
        {
            var receiver = new BlockingCommandAndEventReceiver();
            var sender = new RecordingKernelCommandAndEventSender();

            RegisterForDisposal(KernelEvents.Subscribe(e =>
            {
                receiver.Write(new CommandOrEvent(e));
            }));

            sender.OnSend(async coe =>
            {
                if (coe.Command is { })
                {
                    await SendAsync(coe.Command, CancellationToken.None);
                }

            });

            Sender = sender;
            Receiver = receiver;
        }
    }
}