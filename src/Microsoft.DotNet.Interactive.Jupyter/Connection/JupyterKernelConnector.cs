using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.ValueSharing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal class JupyterKernelConnector : IKernelConnector
    {
        private readonly IJupyterKernelConnection _kernelConnection;
        private readonly IMessageSender _sender;
        private readonly IMessageReceiver _receiver;
        private readonly string _kernelType;

        public JupyterKernelConnector(IJupyterKernelConnection kernelConnection, IMessageSender sender, IMessageReceiver receiver, string kernelType)
        {
            _kernelConnection = kernelConnection;
            _sender = sender;
            _receiver = receiver;
            _kernelType = kernelType;
        }

        public async Task<Kernel> CreateKernelAsync(string kernelName)
        {
            await _kernelConnection.StartAsync(_kernelType);

            MessageToCommandAndEventConnector translator = new MessageToCommandAndEventConnector(_sender, _receiver, _kernelConnection.TargetUri);

            ProxyKernel proxyKernel = new ProxyKernel(
                kernelName,
                translator,
                translator,
                _kernelConnection.TargetUri);

            proxyKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(SubmitCode)));
            proxyKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestValue)));
            proxyKernel.KernelInfo.SupportedKernelCommands.Add(new(nameof(RequestValueInfos)));

            proxyKernel.UseValueSharing(new PythonValueDeclarer());
            proxyKernel.UseWho();
            return proxyKernel;
        }
    }
}
