// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class KernelCommandAndEventPipeStreamReceiver : KernelCommandAndEventReceiverBase
    {
        private readonly PipeStream _reader;

        public KernelCommandAndEventPipeStreamReceiver(PipeStream reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        protected override Task<string> ReadMessageAsync(CancellationToken cancellationToken)
        {
            return _reader.ReadMessageAsync(cancellationToken);
        }
    }
}