// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.SQLite;

public class ConnectSQLiteCommand : ConnectKernelCommand
{
    public ConnectSQLiteCommand()
        : base("sqlite", "Connects to a SQLite database")
    {
        Add(ConnectionStringArgument);
    }

    public Argument<string> ConnectionStringArgument { get; } =
        new("connectionString", "The connection string used to connect to the database");

    public override async Task<IEnumerable<Kernel>> ConnectKernelsAsync(
        KernelInvocationContext context,
        InvocationContext commandLineContext)
    {
        var connectionString = commandLineContext.ParseResult.GetValueForArgument(ConnectionStringArgument);
        var localName = commandLineContext.ParseResult.GetValueForOption(KernelNameOption);
        var kernel1 = new SQLiteKernel($"sql-{localName}", connectionString);
        var kernel = await Task.FromResult<Kernel>(kernel1);
        return new[] { kernel };
    }
}