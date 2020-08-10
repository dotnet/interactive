// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Server
{
    internal static class PipeStreamExtensions
    {
        public static async Task<string> ReadMessageAsync(this PipeStream stream, CancellationToken cancellationToken = default)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(4096);
            try
            {
                var byteMemory = new Memory<byte>(buffer);
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        var readBytes = await stream.ReadAsync(byteMemory, cancellationToken);
                        await ms.WriteAsync(byteMemory.Slice(0, readBytes), cancellationToken);
                    }
                    while (!stream.IsMessageComplete);

                    return System.Text.Encoding.Default.GetString(ms.ToArray());
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static void WriteMessage(this PipeStream stream, string message)
        {
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms);
            writer.Write(message);
            writer.Flush();
            stream.Write(ms.ToArray());
        }
    }
}
