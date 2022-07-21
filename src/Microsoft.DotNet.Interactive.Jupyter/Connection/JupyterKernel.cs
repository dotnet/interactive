using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using System;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;
using System.Threading;
using System.Linq;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal class JupyterKernel : Kernel
    {
        private readonly string _sessionId;

        public JupyterKernel(string name, string kernelType, IMessageSender sender, IMessageReceiver receiver) : base(name, kernelType)
        {
            _sessionId = Guid.NewGuid().ToString();
            RegisterCommandHandler<SubmitCode>((new SubmitCodeHandler(sender, receiver)).HandleAsync);
        }
    }
}
