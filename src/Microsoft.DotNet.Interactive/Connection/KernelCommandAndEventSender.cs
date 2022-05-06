// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Connection;

public class KernelCommandAndEventSender : IKernelCommandAndEventSender
{
    private readonly Func<string, CancellationToken, Task> _sendAsync;

    public KernelCommandAndEventSender(IObserver<string> observer, Uri remoteHostUri) : this(remoteHostUri)
    {
        _sendAsync = NotifyObserverOnNext;

        Task NotifyObserverOnNext(string s, CancellationToken token)
        {
            observer.OnNext(s);
            return Task.CompletedTask;
        }
    }

    public KernelCommandAndEventSender(Func<string, CancellationToken, Task> sendAsync, Uri remoteHostUri) : this(remoteHostUri)
    {
        _sendAsync = sendAsync;
    }

    private KernelCommandAndEventSender(Uri remoteHostUri)
    {
        RemoteHostUri = remoteHostUri;
    }

    public async Task SendAsync(KernelCommand kernelCommand, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var json = KernelCommandEnvelope.Serialize(KernelCommandEnvelope.Create(kernelCommand));

        await _sendAsync(json, cancellationToken);
    }

    public async Task SendAsync(KernelEvent kernelEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var json = KernelEventEnvelope.Serialize(KernelEventEnvelope.Create(kernelEvent));

        await _sendAsync(json, cancellationToken);
    }

    public Uri RemoteHostUri { get; }

    public static KernelCommandAndEventSender FromNamedPipe(
        PipeStream pipeStream, 
        Uri remoteHostUri)
    {
        return new KernelCommandAndEventSender((json, token) =>
        {
            pipeStream.WriteMessage(json);
            return pipeStream.FlushAsync(token);
        }, remoteHostUri);
    }

    public static KernelCommandAndEventSender FromTextWriter(
        TextWriter writer, 
        Uri remoteHostUri)
    {
        writer.NewLine = "\n";

        return new KernelCommandAndEventSender(async (json, token) =>
        {
            await writer.WriteLineAsync(json);
            await writer.FlushAsync();
        }, remoteHostUri);
    }

    public static KernelCommandAndEventSender FromObserver(
        IObserver<string> observer, 
        Uri remoteHostUri)
    {
        return new(observer, remoteHostUri);
    }
}