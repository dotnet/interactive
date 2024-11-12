// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.Data.Sqlite;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Enumerable = System.Linq.Enumerable;

namespace Microsoft.DotNet.Interactive.SQLite;

public class SQLiteKernel :
    Kernel,
    IKernelCommandHandler<SubmitCode>
{
    private readonly string _connectionString;
    private IEnumerable<IEnumerable<IEnumerable<(string name, object value)>>> _tables;

    public SQLiteKernel(string name, string connectionString) : base(name)
    {
        KernelInfo.LanguageName = "SQLite";
        KernelInfo.Description ="""
                                Query a SQLite database
                                """;
                                
        _connectionString = connectionString;
    }

    private DbConnection OpenConnection()
    {
        return new SqliteConnection(_connectionString);
    }

    async Task IKernelCommandHandler<SubmitCode>.HandleAsync(
        SubmitCode submitCode,
        KernelInvocationContext context)
    {
        await using var connection = OpenConnection();
        if (connection.State is not ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var dbCommand = connection.CreateCommand();

        dbCommand.CommandText = submitCode.Code;

        _tables = Execute(dbCommand);

        foreach (var table in _tables)
        {
            var tabularDataResource = table.ToTabularDataResource();

            var explorer = DataExplorer.CreateDefault(tabularDataResource);
            context.Display(explorer);
        }
    }

    private IEnumerable<IEnumerable<IEnumerable<(string name, object value)>>> Execute(IDbCommand command)
    {
        using var reader = command.ExecuteReader();

        do
        {
            var values = new object[reader.FieldCount];
            var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();

            ResolveColumnNameClashes(names);

            // holds the result of a single statement within the query
            var table = new List<(string, object)[]>();

            while (reader.Read())
            {
                reader.GetValues(values);
                var row = new (string, object)[values.Length];
                for (var i = 0; i < values.Length; i++)
                {
                    row[i] = (names[i], values[i]);
                }

                table.Add(row);
            }

            yield return table;
        } while (reader.NextResult());

        void ResolveColumnNameClashes(string[] names)
        {
            var nameCounts = new Dictionary<string, int>(capacity: names.Length);
            for (var i1 = 0; i1 < names.Length; i1++)
            {
                var columnName = names[i1];
                if (nameCounts.TryGetValue(columnName, out var count))
                {
                    nameCounts[columnName] = ++count;
                    names[i1] = columnName + $" ({count})";
                }
                else
                {
                    nameCounts[columnName] = 1;
                }
            }
        }
    }

    public static void AddSQLiteKernelConnectorTo(CompositeKernel kernel)
    {
        kernel.AddConnectDirective(new ConnectSQLiteDirective());

        KernelInvocationContext.Current?.Display(
            new HtmlString(@"<details><summary>Query SQLite databases.</summary>
    <p>This extension adds support for connecting to SQLite databases using the <code>#!connect sqlite</code> magic command. For more information, run a cell using the <code>#!sql</code> magic command.</p>
    </details>"),
            "text/html");
    }

    public static void AddSQLiteKernelConnectorToCurrentRoot()
    {
        if (KernelInvocationContext.Current is { } context &&
            context.HandlingKernel.RootKernel is CompositeKernel root)
        {
            SQLiteKernel.AddSQLiteKernelConnectorTo(root);
        }
    }
}

public class SqlRow : Dictionary<string, object>
{
}