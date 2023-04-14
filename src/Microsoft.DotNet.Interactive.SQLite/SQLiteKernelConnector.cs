﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.SQLite;

public class SQLiteKernelConnector : IKernelConnector
{
    public SQLiteKernelConnector(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public Task<Kernel> CreateKernelAsync(string kernelName)
    {
        var kernel = new SQLiteKernel(
            $"sql-{kernelName}",
            ConnectionString);

        return Task.FromResult<Kernel>(kernel);
    }

    public static void AddSQLiteKernelConnectorToCurrentRoot()
    {
        if (KernelInvocationContext.Current is { } context &&
            context.HandlingKernel.RootKernel is CompositeKernel root)
        {
            AddSQLiteKernelConnectorTo(root);
        }
    }

    public static void AddSQLiteKernelConnectorTo(CompositeKernel kernel)
    {


            kernel.AddKernelConnector(new ConnectSQLiteCommand());

            KernelInvocationContext.Current?.Display(
                new HtmlString(@"<details><summary>Query SQLite databases.</summary>
    <p>This extension adds support for connecting to SQLite databases using the <code>#!connect sqlite</code> magic command. For more information, run a cell using the <code>#!sql</code> magic command.</p>
    </details>"),
                "text/html");

        }
}