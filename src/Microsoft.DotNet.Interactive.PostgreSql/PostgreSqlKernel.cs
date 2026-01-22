// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.ValueSharing;
using Npgsql;
using Enumerable = System.Linq.Enumerable;

namespace Microsoft.DotNet.Interactive.PostgreSql;

public class PostgreSqlKernel :
    Kernel,
    IKernelCommandHandler<SubmitCode>,
    IKernelCommandHandler<RequestValue>,
    IKernelCommandHandler<RequestValueInfos>
{
    private readonly string _connectionString;
    private IEnumerable<IEnumerable<IEnumerable<(string name, object value)>>> _tables;
    private readonly Dictionary<string, object> _resultSets = new(StringComparer.Ordinal);

    public PostgreSqlKernel(string name, string connectionString) : base(name)
    {
        KernelInfo.LanguageName = "PostgreSQL";
        KernelInfo.Description = """
            Query a PostgreSQL database
            """;

        _connectionString = connectionString;
    }

    public override KernelSpecifierDirective KernelSpecifierDirective
    {
        get
        {
            var directive = base.KernelSpecifierDirective;
            directive.Parameters.Add(new("--name"));
            return directive;
        }
    }

    private DbConnection OpenConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    async Task IKernelCommandHandler<SubmitCode>.HandleAsync(
        SubmitCode submitCode,
        KernelInvocationContext context)
    {
        var results = new List<TabularDataResource>();
        try
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

                results.Add(tabularDataResource);
            }
        }
        finally
        {
            submitCode.Parameters.TryGetValue("--name", out var queryName);
            string name = queryName ?? "";
            _resultSets[name] = results;
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

    public static void AddPostgreSqlKernelConnectorToCurrentRoot()
    {
        if (KernelInvocationContext.Current is { } context &&
            context.HandlingKernel.RootKernel is CompositeKernel root)
        {
            root.AddConnectDirective(new ConnectPostgreSqlDirective());

            context.Display(
                new HtmlString("""
                               <details><summary>Query PostgreSQL databases.</summary>
                                   <p>This extension adds support for connecting to PostgreSql databases using the <code>#!connect postgres</code> magic command.</p>
                                   </details>
                               """),
                "text/html");
        }
    }

    private bool TryGetValue<T>(string name, out T value)
    {
        if (_resultSets.TryGetValue(name, out var resultSet) &&
            resultSet is T resultSetT)
        {
            value = resultSetT;
            return true;
        }

        value = default;
        return false;
    }

    Task IKernelCommandHandler<RequestValue>.HandleAsync(RequestValue command, KernelInvocationContext context)
    {
        if (TryGetValue<object>(command.Name, out var value))
        {
            context.Publish(new ValueProduced(
                                value,
                                command.Name,
                                new FormattedValue(
                                    command.MimeType,
                                    value.ToDisplayString(command.MimeType)),
                                command));
        }
        else
        {
            context.Fail(command, message: $"Value '{command.Name}' not found in kernel {Name}");
        }

        return Task.CompletedTask;
    }

    Task IKernelCommandHandler<RequestValueInfos>.HandleAsync(RequestValueInfos command, KernelInvocationContext context)
    {
        var valueInfos = CreateKernelValueInfos(_resultSets, command.MimeType).ToArray();

        context.Publish(new ValueInfosProduced(valueInfos, command));

        return Task.CompletedTask;

        static IEnumerable<KernelValueInfo> CreateKernelValueInfos(IReadOnlyDictionary<string, object> source, string mimeType)
        {
            return source.Keys.Select(key =>
            {
                var formattedValues = FormattedValue.CreateSingleFromObject(
                    source[key],
                    mimeType);

                return new KernelValueInfo(
                    key,
                    formattedValues,
                    type: typeof(IEnumerable<TabularDataResource>));
            });
        }
    }
}
