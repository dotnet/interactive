// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.NamedPipeConnector;

public class ConnectNamedPipeCommand : ConnectKernelCommand
{
    public ConnectNamedPipeCommand() : base("named-pipe", "Connects to a kernel using named pipes")
    {
        AddOption(PipeNameOption);
    }

    public static void AddToRootKernel()
    {
        var compositeKernel = Kernel.Root as CompositeKernel;
        compositeKernel!.AddKernelConnector(new ConnectNamedPipeCommand());
    }

    public Option<string> PipeNameOption { get; } =
        new("--pipe-name", "The name of the named pipe")
        {
            IsRequired = true
        };

    public override async Task<IEnumerable<Kernel>> ConnectKernelsAsync(
        KernelInvocationContext context,
        InvocationContext commandLineContext)
    {
        var pipeName = commandLineContext.ParseResult.GetValueForOption(PipeNameOption);

        var connector = new NamedPipeKernelConnector(pipeName!);

        var localName = commandLineContext.ParseResult.GetValueForOption(KernelNameOption);

        var proxyKernel = await connector.CreateKernelAsync(localName!);

        return new Kernel[] {proxyKernel};
    }
}