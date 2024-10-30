// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.DuckDB;

public class ConnectDuckDBDirective : ConnectKernelDirective<ConnectDuckDBKernel>
{
    private KernelDirectiveParameter ConnectionStringParameter { get; } =
        new("--connection-string", description: "The connection string used to connect to the database")
        {
            AllowImplicitName = true,
            Required = true,
        };

    public ConnectDuckDBDirective() : base("duckdb", "Connects to a DuckDB database")
    {
        Parameters.Add(ConnectionStringParameter);
    }

    public override Task<IEnumerable<Kernel>> ConnectKernelsAsync(ConnectDuckDBKernel connectCommand, KernelInvocationContext context)
    {
        if (string.IsNullOrWhiteSpace(connectCommand.ConnectionString))
        {
            throw new ArgumentException("Provide a valid Connection string");
        }
        var kernel = new DuckDBKernel(connectCommand.ConnectedKernelName, connectCommand.ConnectionString);
        return Task.FromResult<IEnumerable<Kernel>>([kernel]);
    }
}