using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
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
            var kernel = new JupyterKernel(
                kernelName,
                _kernelType,
                _sender,
                _receiver);

            await _kernelConnection.StartAsync(_kernelType);

            return kernel;
        }
    }
}
