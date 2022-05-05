// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Pocket;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Connection.KernelCommandAndEventTextStreamReceiver>;

namespace Microsoft.DotNet.Interactive.Connection;

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

        var readlineTask = _reader.ReadLineAsync();

#if true
        try
        {
            var cts = new CancellationTokenSource();

            var completedTask = await Task.WhenAny(
                                    readlineTask,
                                    cancellationToken.CancellationAsync(cts.Token));

            cts.Cancel();

            if (completedTask == readlineTask)
            {
                var message = await readlineTask;

                if (message is{})
                {
                    return message;
                }
            }

        }
        catch (Exception exception)
        {
           
        }

        return null;
#else
        return await readlineTask;
#endif
    }
}

internal static class TaskHacks
{
    public static async Task CancellationAsync(
        this CancellationToken cancellationToken,
        CancellationToken cancellationToken2)
    {
        try
        {
            await Task.Run(() =>
            {
                return WaitHandle.WaitAny(new[]
                {
                    cancellationToken.WaitHandle,
                    cancellationToken2.WaitHandle,
                });
            });
        }
        catch (Exception exception)
        {
        }
    }
    
}