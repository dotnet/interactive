// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.DotNet.Interactive.Connection;

#nullable enable
namespace Microsoft.DotNet.Interactive.Http;

public class SignalRKernelConnector : IKernelConnector
{
    private readonly CompositeDisposable _disposables = new();
    public string HubUrl { get;  }

    public async Task<Kernel> ConnectKernelAsync(KernelInfo kernelInfo)
    {
        // QUESTION: (ConnectKernelAsync) tests?
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(HubUrl)
            .Build();

        await hubConnection.StartAsync();

        await hubConnection.SendAsync("connect");

        var receiver = new KernelCommandAndEventSignalRHubConnectionReceiver(hubConnection);
        var sender = new KernelCommandAndEventSignalRHubConnectionSender(hubConnection);
        var proxyKernel = new ProxyKernel(kernelInfo.LocalName, receiver, sender);

        var _ = proxyKernel.StartAsync();

        _disposables.Add(receiver);
        _disposables.Add(Disposable.Create( async () =>
        {
            await hubConnection.DisposeAsync();
        }));

        return proxyKernel;
    }

    public SignalRKernelConnector( string hubUrl)
    {
        HubUrl = hubUrl;
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}