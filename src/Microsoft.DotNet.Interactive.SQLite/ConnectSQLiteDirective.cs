// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.SQLite;

public class ConnectSQLiteDirective : ConnectKernelDirective<ConnectSQLiteKernel>
{
    public ConnectSQLiteDirective()
        : base("sqlite", "Connects to a SQLite database")
    {
        AddOption(ConnectionStringParameter);
    }

    public KernelDirectiveParameter ConnectionStringParameter { get; } =
        new("connectionString", "The connection string used to connect to the database");

    public override Task<IEnumerable<Kernel>> ConnectKernelsAsync(
        ConnectSQLiteKernel connectCommand,
        KernelInvocationContext context)
    {
        var connectionString = connectCommand.ConnectionString;
        var localName = connectCommand.ConnectedKernelName;
        var kernel = new SQLiteKernel($"sql-{localName}", connectionString);
        return Task.FromResult<IEnumerable<Kernel>>(new[] { kernel });
    }
}