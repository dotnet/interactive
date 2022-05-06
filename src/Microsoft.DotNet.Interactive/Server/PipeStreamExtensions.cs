// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Connection
{
    internal static class PipeStreamExtensions
    {
        public static async Task<string> ReadMessageAsync(
            this PipeStream stream,
            CancellationToken cancellationToken)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(4096);
            try
            {
                await Task.Yield();

                var byteMemory = new Memory<byte>(buffer);
#if !NETSTANDARD2_0

                await using var ms = new MemoryStream();

                do
                {
                    var readBytes = await stream.ReadAsync(byteMemory, cancellationToken);
                    await ms.WriteAsync(byteMemory[..readBytes], cancellationToken);
                } while (!cancellationToken.IsCancellationRequested &&
                         !stream.IsMessageComplete);
#else
                using var ms = new MemoryStream();
                do
                {
                    var readBytes = await stream.ReadAsync(buffer, 0, (int)buffer.Length, cancellationToken);
                    await ms.WriteAsync(buffer, 0, readBytes, cancellationToken);
                }
                while (!cancellationToken.IsCancellationRequested && 
                       !stream.IsMessageComplete);
#endif

                return Encoding.Default.GetString(ms.ToArray());
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
            stream.Write(ms.ToArray(), 0, (int)ms.Length);
        }
    }
}