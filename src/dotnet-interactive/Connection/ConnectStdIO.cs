// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.App.Connection
{
    public class ConnectStdIoCommand : ConnectKernelCommand
    {
        public ConnectStdIoCommand() : base("stdio",
                                            "Connects to a kernel using the stdio protocol")
        {
            AddOption(CommandOption);
            AddOption(WorkingDirectoryOption);
        }

        public Option<DirectoryInfo> WorkingDirectoryOption { get; } =
            new("--working-directory", 
                getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()), 
                "The working directory");

        public Option<string[]> CommandOption { get; } =
            new("--command", "The command to execute")
            {
                AllowMultipleArgumentsPerToken = true,
                IsRequired = true,
            };

        public override Task<Kernel> ConnectKernelAsync(
            KernelInvocationContext context,
            InvocationContext commandLineContext)
        {
            var command = commandLineContext.ParseResult.GetValueForOption(CommandOption);
            var workingDir = commandLineContext.ParseResult.GetValueForOption(WorkingDirectoryOption);
            
            var connector = new StdIoKernelConnector(command, workingDir);
            
            var localName = commandLineContext.ParseResult.GetValueForOption(KernelNameOption);

            return connector.CreateKernelAsync(localName);
        }
    }
}