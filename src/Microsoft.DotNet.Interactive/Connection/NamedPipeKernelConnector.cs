// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Pipes;
using System.Reactive.Disposables;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using CompositeDisposable = Pocket.CompositeDisposable;

#nullable enable

namespace Microsoft.DotNet.Interactive.Connection;

public class NamedPipeKernelConnector : IDisposable
{
    private KernelCommandAndEventReceiver? _receiver;
    private KernelCommandAndEventSender? _sender;
    private NamedPipeClientStream? _clientStream;
    private RefCountDisposable? _refCountDisposable = null;

    public NamedPipeKernelConnector(string pipeName)
    {
        PipeName = pipeName;
        RemoteHostUri = new Uri($"kernel://{PipeName}");
    }

    public string PipeName { get; }

    public Uri RemoteHostUri { get; }

    public async Task<ProxyKernel> CreateKernelAsync(string localName)
    {
        ProxyKernel? proxyKernel;

        if (_receiver is null)
        {
            _clientStream = new NamedPipeClientStream(
                ".",
                PipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous,
                TokenImpersonationLevel.Impersonation);

            await _clientStream.ConnectAsync();

            _clientStream.ReadMode = PipeTransmissionMode.Message;
         
            _receiver = KernelCommandAndEventReceiver.FromNamedPipe(_clientStream);
            _sender = KernelCommandAndEventSender.FromNamedPipe(
                _clientStream,
                RemoteHostUri);

            _refCountDisposable = new RefCountDisposable(new CompositeDisposable
            {
                () => _clientStream.Dispose(),
                () => _receiver.Dispose()
            });

            proxyKernel = new ProxyKernel(
                localName, 
                _sender, 
                _receiver, 
                new Uri(RemoteHostUri, localName));
            proxyKernel.RegisterForDisposal(_refCountDisposable);
        }
        else
        {
            proxyKernel = new ProxyKernel(
                localName,
                _sender,
                _receiver, 
                new Uri(RemoteHostUri, localName));

            proxyKernel.RegisterForDisposal(_refCountDisposable!.GetDisposable());
        }

        var destinationUri = new Uri(RemoteHostUri, localName);

        await _sender!.SendAsync(
            new RequestKernelInfo(destinationUri),
            CancellationToken.None);

        return proxyKernel;
    }

    public void Dispose() => _refCountDisposable?.Dispose();
}