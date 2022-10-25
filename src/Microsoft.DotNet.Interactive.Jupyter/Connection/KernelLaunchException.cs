using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter.Connection
{
    internal class KernelLaunchException : Exception
    {
        public KernelLaunchException(string reason): base($"Kernel launch failed due to {reason}")
        {
        }
    }
}
