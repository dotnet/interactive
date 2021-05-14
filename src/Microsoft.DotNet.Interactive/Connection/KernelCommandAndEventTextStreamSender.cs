// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Connection
{
    public interface IKernelCommandAndEventSender
    {
        Task SendAsync(KernelCommand kernelCommand);
        Task SendAsync(KernelEvent kernelEvent);
    }

    public class KernelCommandAndEventTextStreamSender : IKernelCommandAndEventSender
    {
        private readonly TextWriter _writer;

        public KernelCommandAndEventTextStreamSender(TextWriter writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public async Task SendAsync(KernelCommand kernelCommand)
        {
            await _writer.WriteAsync(KernelCommandEnvelope.Serialize(KernelCommandEnvelope.Create(kernelCommand)));

            await _writer.WriteAsync(Delimiter);

            await _writer.FlushAsync();
        }

        public async Task SendAsync(KernelEvent kernelEvent)
        {
            await _writer.WriteAsync(KernelEventEnvelope.Serialize(KernelEventEnvelope.Create(kernelEvent)));

            await _writer.WriteAsync(Delimiter);
        }

        public static string Delimiter => "\r\n";
    }
}