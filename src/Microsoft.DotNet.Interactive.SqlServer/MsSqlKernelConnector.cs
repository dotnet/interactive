﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.SqlServer;

public class MsSqlKernelConnector : IKernelConnector
{
    public MsSqlKernelConnector(bool createDbContext, string connectionString)
    {
        CreateDbContext = createDbContext;
        ConnectionString = connectionString;
    }

    public bool CreateDbContext { get; }

    public string ConnectionString { get; }

    public string PathToService { get; set; }

    public async Task<Kernel> CreateKernelAsync(string kernelName)
    {
        if (string.IsNullOrWhiteSpace(PathToService))
        {
            throw new InvalidOperationException($"{nameof(PathToService)} cannot be null or whitespace.");
        }

        var client = new ToolsServiceClient(PathToService, $"--parent-pid {Environment.ProcessId}");

        var kernel = new MsSqlKernel(
                $"sql-{kernelName}",
                ConnectionString,
                client)
            .UseValueSharing();

        kernel.RegisterForDisposal(client);

        await kernel.ConnectAsync();

        return kernel;
    }
}