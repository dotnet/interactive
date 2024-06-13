// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.Kql;

public class ConnectKqlDirective : ConnectKernelDirective<ConnectKqlKernel>
{
    private readonly string ResolvedToolsServicePath;

    public ConnectKqlDirective(string resolvedToolsServicePath)
        : base("kql", "Connects to a Microsoft Kusto Server database")
    {
        ResolvedToolsServicePath = resolvedToolsServicePath;
        AddOption(ClusterParameter);
        AddOption(DatabaseParameter);
    }

    public KernelDirectiveParameter ClusterParameter { get; } =
        new("--cluster",
            "The cluster used to connect")
        {
            Required = true
        };

    public KernelDirectiveParameter DatabaseParameter { get; } =
        new("--database",
            "The database to query");

    public override async Task<IEnumerable<Kernel>> ConnectKernelsAsync(
        ConnectKqlKernel connectCommand,
        KernelInvocationContext context)
    {
        var connector = new KqlKernelConnector(
            connectCommand.Cluster,
            connectCommand.Database);

        connector.PathToService = ResolvedToolsServicePath;

        var localName = connectCommand.ConnectedKernelName;

        var found = context?.HandlingKernel?.RootKernel.FindKernelByName($"kql-{localName}") is not null;

        if (found)
        {
            throw new InvalidOperationException(
                $"A kernel with name {connectCommand.ConnectedKernelName} is already present. Use a different value for the {KernelNameParameter.Name} parameter.");
        }

        var kernel = await connector.CreateKernelAsync(localName);

        return new[] { kernel };
    }
}