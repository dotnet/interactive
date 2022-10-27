using System;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal class KernelStartException : Exception
    {
        public KernelStartException(string kernelType, string reason): base($"Kernel {kernelType} failed to start. {reason}")
        {
        }
    }
}
