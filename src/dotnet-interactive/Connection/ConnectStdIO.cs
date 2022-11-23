﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.App.Connection
{
    public class ConnectStdIoCommand : ConnectKernelCommand
    {
        private static int KernelHostAuthoritySuffix = 1;
        private Uri _kernelHostUri;

        public ConnectStdIoCommand(Uri kernelHostUri) : base("stdio",
                                            "Connects to a kernel using the stdio protocol")
        {
            _kernelHostUri = kernelHostUri;

            AddOption(CommandOption);
            AddOption(WorkingDirectoryOption);
            AddOption(KernelHostUriOption);
        }

        private string CreateKernelHostAuthority()
        {
            var suffix = Interlocked.Increment(ref KernelHostAuthoritySuffix);
            return $"{_kernelHostUri.Authority}-{suffix}";
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

        public Option<string> KernelHostUriOption { get; } = new(
                "--kernel-host",
                parseArgument: result => result.Tokens.Count == 0 ? null : result.Tokens[0].Value,
                isDefault: true,
                description: "Name of the kernel host.");

        public override Task<Kernel> ConnectKernelAsync(
            KernelInvocationContext context,
            InvocationContext commandLineContext)
        {
            var command = commandLineContext.ParseResult.GetValueForOption(CommandOption);
            var workingDir = commandLineContext.ParseResult.GetValueForOption(WorkingDirectoryOption);
            var kernelHostAuthority = commandLineContext.ParseResult.GetValueForOption(KernelHostUriOption) ?? CreateKernelHostAuthority();
            var kernelHostUri = KernelHost.CreateHostUri(kernelHostAuthority);
            
            var connector = new StdIoKernelConnector(command, kernelHostUri, workingDir);
            
            var localName = commandLineContext.ParseResult.GetValueForOption(KernelNameOption);

            return connector.CreateKernelAsync(localName);
        }
    }
}
