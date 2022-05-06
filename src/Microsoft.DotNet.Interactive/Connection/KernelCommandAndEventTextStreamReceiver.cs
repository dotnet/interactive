// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Connection;

public class KernelCommandAndEventTextStreamReceiver : KernelCommandAndEventDeserializingReceiverBase
{
    private readonly TextReader _reader;

    public KernelCommandAndEventTextStreamReceiver(TextReader reader)
    {
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    protected override async Task<string> ReadMessageAsync(CancellationToken cancellationToken)
    {
        return await _reader.ReadLineAsync(cancellationToken);
    }
}

internal static class TextReaderExtensions
{
    public static async Task<string> ReadLineAsync(
        this TextReader textReader,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var readlineTask = textReader.ReadLineAsync();

        var cts = new CancellationTokenSource();

        var completedTask = await Task.WhenAny(
                                readlineTask,
                                cancellationToken.CancellationAsync(cts.Token));

        cts.Cancel();

        if (completedTask == readlineTask)
        {
            var message = await readlineTask;

            if (message is { })
            {
                return message;
            }
        }

        return null;
    }

    private static async Task CancellationAsync(
        this CancellationToken cancellationToken,
        CancellationToken cancellationToken2)
    {
        await Task.Run(() =>
        {
            try
            {
                WaitHandle.WaitAny(new[]
                {
                    cancellationToken.WaitHandle,
                    cancellationToken2.WaitHandle,
                });
            }
            catch
            {
            }
        });
    }
}