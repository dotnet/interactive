using System;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.DotNet.Interactive.App
{
    public class KernelHub : Hub
    {
        private readonly IKernel _kernel;

        public KernelHub(IKernel kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

    }
}