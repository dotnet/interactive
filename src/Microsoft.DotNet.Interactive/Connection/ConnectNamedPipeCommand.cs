// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Documents;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class ConnectNamedPipeCommand : ConnectKernelCommand<NamedPipeKernelConnector>
    {
        public ConnectNamedPipeCommand() : base("named-pipe",
                                         "Connects to a kernel using named pipes")
        {
            AddOption(new Option<string>("--pipe-name", "The name of the named pipe"));
        }

        public override Task<Kernel> ConnectKernelAsync(KernelName kernelName, NamedPipeKernelConnector connector,
            KernelInvocationContext context)
        {
            return connector.ConnectKernelAsync(kernelName);
        }
    }
}