using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal interface IJupyterKernelConnection : IDisposable
    {
        Uri TargetUri { get; }

        Task StartAsync(string kernelType);
    }
}
