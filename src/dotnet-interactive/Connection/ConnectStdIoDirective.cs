// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.App.Connection;

public class ConnectStdIoDirective : ConnectKernelDirective<ConnectStdio>
{
    private static int _kernelHostAuthoritySuffix = 1;
    private readonly Uri _kernelHostUri;

    public ConnectStdIoDirective(Uri kernelHostUri) : base("stdio",
        "Connects to a kernel using the stdio protocol")
    {
        _kernelHostUri = kernelHostUri;

        Parameters.Add(CommandParameter);
        Parameters.Add(WorkingDirectoryParameter);
        Parameters.Add(KernelHostUriParameter);
    }

    private string CreateKernelHostAuthority()
    {
        var suffix = Interlocked.Increment(ref _kernelHostAuthoritySuffix);
        return $"{_kernelHostUri.Authority}-{suffix}";
    }

    public KernelDirectiveParameter WorkingDirectoryParameter { get; } =
        new("--working-directory",
            // FIX: (WorkingDirectoryParameter)     getDefaultValue: () => new DirectoryInfo(Directory.GetCurrentDirectory()),
            "The working directory");

    public KernelDirectiveParameter CommandParameter { get; } =
        new("--command", "The command to execute")
        {
            Required = true
        };

    public KernelDirectiveParameter KernelHostUriParameter { get; } = new(
        "--kernel-host",
        description: "Name of the kernel host.");

    public override async Task<IEnumerable<Kernel>> ConnectKernelsAsync(
        ConnectStdio connectCommand,
        KernelInvocationContext context)
    {
        var command = connectCommand.Command;
        var workingDir = connectCommand.WorkingDirectory;
        var kernelHostAuthority = connectCommand.KernelHostUri ?? CreateKernelHostAuthority();
        var kernelHostUri = KernelHost.CreateHostUri(kernelHostAuthority);

        var localName = connectCommand.ConnectedKernelName;

        var connector = new StdIoKernelConnector(command, rootProxyKernelLocalName: localName, kernelHostUri, workingDir);

        var kernel = await connector.CreateRootProxyKernelAsync();

        return new[] { kernel };
    }
}