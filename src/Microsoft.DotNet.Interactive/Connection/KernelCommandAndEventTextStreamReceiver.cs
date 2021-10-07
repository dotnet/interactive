// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class KernelCommandAndEventTextReceiver : InteractiveProtocolKernelCommandAndEventReceiverBase
    {
        private readonly TextReader _reader;

        public KernelCommandAndEventTextReceiver(TextReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }


        protected override Task<string> ReadMessageAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _reader.ReadLineAsync();
        }
    }
}