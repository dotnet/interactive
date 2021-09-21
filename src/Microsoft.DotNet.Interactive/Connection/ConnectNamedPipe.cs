// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class ConnectNamedPipe : ConnectKernelCommand<NamedPipeConnection>
    {
        public ConnectNamedPipe() : base("named-pipe",
                                         "Connects to a kernel using named pipes")
        {
            AddOption(new Option<string>("--pipe-name", "The name of the named pipe"));
        }

        public override Task<Kernel> ConnectKernelAsync(NamedPipeConnection connection, KernelInvocationContext context)
        {
            return connection.ConnectKernelAsync();
        }
    }
}