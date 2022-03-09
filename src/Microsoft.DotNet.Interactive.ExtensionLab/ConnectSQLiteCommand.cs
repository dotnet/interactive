// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class ConnectSQLiteCommand : ConnectKernelCommand
    {
        public ConnectSQLiteCommand()
            : base("sqlite", "Connects to a SQLite database")
        {
            Add(ConnectionStringArgument);
        }

        public Argument<string> ConnectionStringArgument { get; } =
            new("connectionString", "The connection string used to connect to the database");

        public override Task<Kernel> ConnectKernelAsync(
            KernelInvocationContext context,
            InvocationContext commandLineContext)
        {
            var connectionString = commandLineContext.ParseResult.GetValueForArgument(ConnectionStringArgument);
            var connector = new SQLiteKernelConnector(connectionString);
            var localName = commandLineContext.ParseResult.GetValueForOption(KernelNameOption);
            return connector.ConnectKernelAsync(localName);
        }
    }
}