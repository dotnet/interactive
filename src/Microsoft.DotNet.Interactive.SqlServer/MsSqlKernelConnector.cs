﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.SqlServer;

public class MsSqlKernelConnector : KernelConnectorBase
{
    public bool CreateDbContext { get; }

    public string ConnectionString { get; }

    public string PathToService { get; set; }

    public override async Task<Kernel> ConnectKernelAsync(KernelInfo kernelInfo)
    {
        if (string.IsNullOrWhiteSpace(PathToService))
        {
            throw new InvalidOperationException($"{nameof(PathToService)} cannot be null or whitespace.");
        }

        var sqlClient = new ToolsServiceClient(PathToService, $"--parent-pid {Environment.ProcessId}");

        var kernel = new MsSqlKernel(
                $"sql-{kernelInfo}",
                ConnectionString,
                sqlClient)
            .UseValueSharing();

        kernel.RegisterForDisposal(sqlClient);

        await kernel.ConnectAsync();

        return kernel;
    }

    public MsSqlKernelConnector(bool createDbContext, string connectionString)
    {
        CreateDbContext = createDbContext;
        ConnectionString = connectionString;
    }
}