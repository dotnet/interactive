// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class KernelCommandAndEventTextStreamSender : IKernelCommandAndEventSender
    {
        private readonly TextWriter _writer;

        public KernelCommandAndEventTextStreamSender(
            TextWriter writer, 
            Uri remoteHostUri)
        {
            RemoteHostUri = remoteHostUri;
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _writer.NewLine = "\n";
        }

        public Uri RemoteHostUri { get; }

        public async Task SendAsync(KernelCommand kernelCommand, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // the entirety of the content (envelope and the trailing newline) needs to be sent atomically to prevent interleaving between rapid outputs
            var content = KernelCommandEnvelope.Serialize(KernelCommandEnvelope.Create(kernelCommand));
            
            await _writer.WriteLineAsync(content);
            await _writer.FlushAsync();
        }

        public async Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // the entirety of the content (envelope and the trailing newline) needs to be sent atomically to prevent interleaving between rapid outputs
            var content = KernelEventEnvelope.Serialize(KernelEventEnvelope.Create(kernelEvent));
            await _writer.WriteLineAsync(content);
            await _writer.FlushAsync();
        }
    }
}