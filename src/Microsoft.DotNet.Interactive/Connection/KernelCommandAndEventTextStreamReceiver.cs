// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class KernelCommandAndEventTextReaderReceiver : KernelCommandAndEventDeserializingReceiverBase
    {
        private readonly TextReader _reader;

        public KernelCommandAndEventTextReaderReceiver(TextReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }


        protected override Task<string> ReadMessageAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var message = _reader.ReadLineAsync();
            return message;
        }
    }
}