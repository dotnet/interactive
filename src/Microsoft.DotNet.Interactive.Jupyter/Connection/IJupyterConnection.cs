using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal interface IJupyterConnection : IDisposable
    {
        Uri TargetUri { get; }

        Task<IJupyterKernelConnection> CreateKernelConnectionAsync(string kernelType);
    }
}
