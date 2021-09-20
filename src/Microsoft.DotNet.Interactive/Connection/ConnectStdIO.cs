// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class ConnectStdIoCommand : ConnectKernelCommand<StdIoKernelConnector>
    {
        public ConnectStdIoCommand() : base("stdio",
                                     "Connects to a kernel using the stdio protocol")
        {
            Add(new Argument<string[]>("command", "The command to execute"));
            Add(new Option<DirectoryInfo>("--working-directory", () => new DirectoryInfo(Directory.GetCurrentDirectory()), "The working directory"));
            Add(new Option<bool>("--wait-for-kernel-ready-event", "Wait for a kernel ready event before continuing"));
        }

        public override Task<Kernel> ConnectKernelAsync(KernelName kernelName, StdIoKernelConnector kernelConnector,
            KernelInvocationContext context)
        {
            return kernelConnector.ConnectKernelAsync(kernelName);
        }
    }
}
