// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class KernelCommandAndEventPipeStreamSender : IKernelCommandAndEventSender
    {
        private readonly PipeStream _sender;

        public KernelCommandAndEventPipeStreamSender(PipeStream sender)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }

        public async Task SendAsync(KernelCommand kernelCommand, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sender.WriteMessage(KernelCommandEnvelope.Serialize(KernelCommandEnvelope.Create(kernelCommand)));

            await _sender.FlushAsync(cancellationToken);
        }

        public async Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _sender.WriteMessage(KernelEventEnvelope.Serialize(KernelEventEnvelope.Create(kernelEvent)));

            await _sender.FlushAsync(cancellationToken);
        }
    }
}