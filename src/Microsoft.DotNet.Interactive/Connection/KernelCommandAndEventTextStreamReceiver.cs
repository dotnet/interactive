// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pocket;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Connection.KernelCommandAndEventTextStreamReceiver>;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class KernelCommandAndEventTextStreamReceiver : KernelCommandAndEventDeserializingReceiverBase
    {
        private readonly TextReader _reader;
        private readonly string _name;

        public KernelCommandAndEventTextStreamReceiver(
            TextReader reader,
            string name = null)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _name = name;
            
            Log.Info($"Created receiver \"{name}\" ({GetHashCode()})");
        }

        protected override async Task<string> ReadMessageAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message = await _reader.ReadLineAsync( );
            
            return message;
        }
    }
}