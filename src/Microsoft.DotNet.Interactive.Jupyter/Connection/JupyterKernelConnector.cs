using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.ValueSharing;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive;

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
            var remoteUri = _jupyterConnection.TargetUri;
            var kernelConnection = await _jupyterConnection.CreateKernelConnectionAsync(_kernelType);
            var sender = kernelConnection.Sender;
            var receiver = kernelConnection.Receiver;

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

            if (kernelInfoProduced.KernelInfo.LanguageName == LanguageNameValues.Python)
            {
                proxyKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestValue)));
                proxyKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestValueInfos)));
                proxyKernel.UseValueSharing(new PythonValueDeclarer());
                proxyKernel.UseWho();
            }

            proxyKernel.RegisterForDisposal(kernelConnection);
            proxyKernel.RegisterForDisposal(commandOrEventHandler);

            return proxyKernel;
        }
    }
}
