// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.Kql;

public class ConnectKqlCommand : ConnectKernelCommand
{
    private readonly string ResolvedToolsServicePath;

    public ConnectKqlCommand(string resolvedToolsServicePath)
        : base("kql", "Connects to a Microsoft Kusto Server database")
    {
        ResolvedToolsServicePath = resolvedToolsServicePath;
        Add(ClusterOption);
        Add(DatabaseOption);
    }

    public Option<string> ClusterOption { get; } =
        new("--cluster",
            "The cluster used to connect") { IsRequired = true };

    public Option<string> DatabaseOption { get; } =
        new("--database",
            "The database to query");

    public override async Task<IEnumerable<Kernel>> ConnectKernelsAsync(
        KernelInvocationContext context,
        InvocationContext commandLineContext)
    {
        var connector = new KqlKernelConnector(
            commandLineContext.ParseResult.GetValueForOption(ClusterOption),
            commandLineContext.ParseResult.GetValueForOption(DatabaseOption));

        connector.PathToService = ResolvedToolsServicePath;

        var localName = commandLineContext.ParseResult.GetValueForOption(KernelNameOption);

        var found = context?.HandlingKernel?.RootKernel.FindKernelByName($"kql-{localName}") is not null;

        if (found)
        {
            throw new InvalidOperationException(
                $"A kernel with name {localName} is already present. Use a different value for the --{KernelNameOption.Name} option.");
        }

        var kernel = await connector.CreateKernelAsync(localName);

        return new [] {kernel};
    }
}