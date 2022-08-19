using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Messaging.Comms;
using Microsoft.DotNet.Interactive.Jupyter.ValueSharing;
using Microsoft.DotNet.Interactive.ValueSharing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal class JupyterKernelConnector : IKernelConnector
    {
        private readonly IJupyterConnection _jupyterConnection;
        private readonly string _kernelType;

        public JupyterKernelConnector(IJupyterConnection jupyterConnection, string kernelType)
        {
            _jupyterConnection = jupyterConnection;
            _kernelType = kernelType;
        }

        public async Task<Kernel> CreateKernelAsync(string kernelName)
        {
            var kernelConnection = await _jupyterConnection.CreateKernelConnectionAsync(_kernelType);
            var remoteUri = _jupyterConnection.TargetUri;
            var sender = kernelConnection.Sender;
            var receiver = kernelConnection.Receiver;
            var commsManager = new CommsManager(sender, receiver);

            MessageToCommandAndEventConnector commandOrEventHandler = new(sender, receiver, remoteUri);

            ProxyKernel proxyKernel = new(kernelName, commandOrEventHandler, commandOrEventHandler, remoteUri);
            proxyKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(SubmitCode)));

            KernelInfoProduced kernelInfoProduced = null;
            Task waitForKernelInfoProduced = Task.Run(async () =>
            {
                while (kernelInfoProduced == null)
                {
                    await Task.Delay(200);
                }
            });

            commandOrEventHandler.Select(coe => coe.Event)
                                   .OfType<KernelInfoProduced>()
                                   .Take(1)
                                   .Subscribe(e => kernelInfoProduced = e);

            // start the kernel connection and request kernel info
            await kernelConnection.StartAsync();
            await commandOrEventHandler.SendAsync(new RequestKernelInfo(), CancellationToken.None);
            await waitForKernelInfoProduced;


            proxyKernel.RegisterForDisposal(kernelConnection);
            proxyKernel.RegisterForDisposal(commandOrEventHandler);
            proxyKernel.RegisterForDisposal(commsManager);

            var getValueAdapter = new LanguageValueAdapterFactory(sender, receiver, commsManager);
            var valueAdapter = await getValueAdapter.GetValueAdapter(kernelInfoProduced.KernelInfo);

            if (valueAdapter is not null)
            {
                commandOrEventHandler.RegisterCommandHandler<RequestValue>(valueAdapter.HandleCommandAsync);
                commandOrEventHandler.RegisterCommandHandler<RequestValueInfos>(valueAdapter.HandleCommandAsync);
                commandOrEventHandler.RegisterCommandHandler<SetValue>(valueAdapter.HandleCommandAsync);

                proxyKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(SetValue)));
                proxyKernel.UseValueSharing(new DefaultKernelValueDeclarer());
                proxyKernel.UseWho();

                proxyKernel.RegisterForDisposal(valueAdapter);
            }

            return proxyKernel;
        }
    }
}
