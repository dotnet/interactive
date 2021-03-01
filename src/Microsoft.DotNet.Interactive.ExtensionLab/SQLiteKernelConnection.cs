﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class SQLiteKernelConnection : ConnectKernelCommand<SQLiteConnectionOptions>
    {
        public SQLiteKernelConnection()
            : base("sqlite", "Connects to a SQLite database")
        {
            Add(new Argument<string>("connectionString", "The connection string used to connect to the database"));
        }

        public override Task<Kernel> CreateKernelAsync(
            SQLiteConnectionOptions options,
            KernelInvocationContext context)
        {
            var kernel = new SQLiteKernel(
                $"sql-{options.KernelName}",
                options.ConnectionString);

            return Task.FromResult<Kernel>(kernel);
        }
    }
}