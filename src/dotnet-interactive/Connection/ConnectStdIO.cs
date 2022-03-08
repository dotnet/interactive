// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.App.Connection
{
    public class ConnectStdIoCommand : ConnectKernelCommand<StdIoKernelConnector>
    {
        public ConnectStdIoCommand() : base("stdio",
                                     "Connects to a kernel using the stdio protocol")
        {
            AddOption(new Option<string[]>("--command", "The command to execute")
            {
                AllowMultipleArgumentsPerToken = true,
                IsRequired = true,
            });
            AddOption(new Option<DirectoryInfo>("--working-directory", () => new DirectoryInfo(Directory.GetCurrentDirectory()), "The working directory"));
        }

        public override Task<Kernel> ConnectKernelAsync(
            KernelInfo kernelInfo, 
            StdIoKernelConnector kernelConnectorBase,
            KernelInvocationContext context)
        {
            return kernelConnectorBase.ConnectKernelAsync(kernelInfo.LocalName);
        }
    }
}
