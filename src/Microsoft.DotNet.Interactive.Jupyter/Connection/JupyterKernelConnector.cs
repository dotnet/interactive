// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Messaging.Comms;
using Microsoft.DotNet.Interactive.Jupyter.ValueSharing;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection;

public class JupyterKernelConnector : IKernelConnector
{
    private readonly IJupyterConnection _jupyterConnection;
    private readonly string _kernelSpecName;

    public JupyterKernelConnector(IJupyterConnection jupyterConnection, string kernelSpecName)
    {
        _jupyterConnection = jupyterConnection;
        _kernelSpecName = kernelSpecName;
    }

    public async Task<Kernel> CreateKernelAsync(string kernelName)
    {
        var kernelConnection = await _jupyterConnection.CreateKernelConnectionAsync(_kernelSpecName);
        var remoteUri = kernelConnection.Uri;
        var sender = kernelConnection.Sender;
        var receiver = kernelConnection.Receiver;
        var commsManager = new CommsManager(sender, receiver);

        var coeSenderAndReceiver = new JupyterConnectionCommandAndEventSenderAndReceiver(sender, receiver, remoteUri);

        ProxyKernel proxyKernel = new(kernelName, coeSenderAndReceiver, coeSenderAndReceiver, remoteUri);
        proxyKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(SubmitCode)));

        KernelInfoProduced kernelInfoProduced = null;
        Task waitForKernelInfoProduced = Task.Run(async () =>
        {
            while (kernelInfoProduced == null)
            {
                await Task.Delay(200);
            }
        });

        coeSenderAndReceiver.Select(coe => coe.Event)
                               .OfType<KernelInfoProduced>()
                               .Take(1)
                               .Subscribe(e => kernelInfoProduced = e);

        // start the kernel connection and request kernel info
        await kernelConnection.StartAsync();
        await coeSenderAndReceiver.SendAsync(new RequestKernelInfo(), CancellationToken.None);
        await waitForKernelInfoProduced;


        proxyKernel.RegisterForDisposal(kernelConnection);
        proxyKernel.RegisterForDisposal(coeSenderAndReceiver);
        proxyKernel.RegisterForDisposal(commsManager);

        var getValueAdapter = new LanguageValueAdapterFactory(sender, receiver, commsManager);
        var valueAdapter = await getValueAdapter.GetValueAdapter(kernelInfoProduced.KernelInfo);

        if (valueAdapter is not null)
        {
            coeSenderAndReceiver.RegisterCommandHandler<RequestValue>(valueAdapter.HandleCommandAsync);
            coeSenderAndReceiver.RegisterCommandHandler<RequestValueInfos>(valueAdapter.HandleCommandAsync);
            coeSenderAndReceiver.RegisterCommandHandler<SendValue>(valueAdapter.HandleCommandAsync);

            proxyKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(SendValue)));
            proxyKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestValue)));
            proxyKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestValueInfos)));
            proxyKernel.UseValueSharing();
            proxyKernel.UseWho();

            proxyKernel.RegisterForDisposal(valueAdapter);
        }

        return proxyKernel;
    }
}
