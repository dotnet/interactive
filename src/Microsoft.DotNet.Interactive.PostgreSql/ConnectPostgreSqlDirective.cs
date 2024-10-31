// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.PostgreSql;

public class ConnectPostgreSqlDirective : ConnectKernelDirective<ConnectPostgreSqlKernel>
{
    public ConnectPostgreSqlDirective()
        : base("postgres", "Connects to a PostgreSQL database")
    {
        Parameters.Add(ConnectionStringParameter);
    }

    public KernelDirectiveParameter ConnectionStringParameter { get; } =
        new("--connection-string", "The connection string used to connect to the database")
        {
            AllowImplicitName = true,
            Required = true,
            TypeHint = "connectionstring-postgresql"
        };

    public override Task<IEnumerable<Kernel>> ConnectKernelsAsync(
        ConnectPostgreSqlKernel connectCommand,
        KernelInvocationContext context)
    {
        var connectionString = connectCommand.ConnectionString;
        var localName = connectCommand.ConnectedKernelName;
        var kernel = new PostgreSqlKernel($"sql-{localName}", connectionString);
        return Task.FromResult<IEnumerable<Kernel>>([kernel]);
    }
}