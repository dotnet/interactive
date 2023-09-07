// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.DotNet.Interactive.Connection;

#nullable enable
namespace Microsoft.DotNet.Interactive.Http;

public class SignalRKernelConnector
{
    public SignalRKernelConnector(Uri hubUrl)
    {
        HubUrl = hubUrl;
    }

    public Uri HubUrl { get; }

    public async Task<ProxyKernel> CreateKernelAsync(string kernelName)
    {
        // QUESTION: (ConnectKernelAsync) tests?
        var hubConnection = new HubConnectionBuilder()
                            .WithUrl(HubUrl)
                            .Build();

        await hubConnection.StartAsync();

        await hubConnection.SendAsync("connect");

        var subject = new Subject<string>();

        var disposables = new CompositeDisposable
        {
            hubConnection.On<string>("kernelCommandFromRemote", e => subject.OnNext(e)),
            hubConnection.On<string>("kernelEventFromRemote", e => subject.OnNext(e)),
            hubConnection.On<string>("kernelEvent", e => subject.OnNext(e)),
        };

        var receiver = KernelCommandAndEventReceiver.FromObservable(subject);
        
        var sender = new KernelCommandAndEventSignalRHubConnectionSender(hubConnection);

        var proxyKernel = new ProxyKernel(
            kernelName, 
            sender, 
            receiver, 
            new Uri(HubUrl, kernelName));
        
        proxyKernel.RegisterForDisposal(disposables);
        proxyKernel.RegisterForDisposal(receiver);
        proxyKernel.RegisterForDisposal(Disposable.Create(async () => await hubConnection.DisposeAsync()));

        return proxyKernel;
    }
}