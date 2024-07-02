// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.NamedPipeConnector;

public class ConnectNamedPipeDirective : ConnectKernelDirective<ConnectNamedPipe>
{
    public ConnectNamedPipeDirective() : base("named-pipe", "Connects to a kernel using named pipes")
    {
        Parameters.Add(PipeNameParameter);
    }

    public KernelDirectiveParameter PipeNameParameter { get; } =
        new("--pipe-name", "The name of the named pipe")
        {
            Required = true
        };

    public override async Task<IEnumerable<Kernel>> ConnectKernelsAsync(
        ConnectNamedPipe connectCommand,
        KernelInvocationContext context)
    {
        // FIX: (ConnectKernelsAsync) register the connector for disposal

        var pipeName = connectCommand.PipeName;

        var connector = new NamedPipeKernelConnector(pipeName);

        var localName = connectCommand.ConnectedKernelName;

        var proxyKernel = await connector.CreateKernelAsync(localName);

        return new Kernel[] { proxyKernel };
    }
}