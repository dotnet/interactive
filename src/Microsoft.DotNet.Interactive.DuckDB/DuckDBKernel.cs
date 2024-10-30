// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DuckDB.NET.Data;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.ValueSharing;

namespace Microsoft.DotNet.Interactive.DuckDB;

public class DuckDBKernel : Kernel, 
    IKernelCommandHandler<SubmitCode>,
    IKernelCommandHandler<RequestValueInfos>,
    IKernelCommandHandler<RequestValue>
    

{
    private IEnumerable<IEnumerable<IEnumerable<(string name, object value)>>>? _tables;
    private readonly Dictionary<string, object> _resultSets  = new();
    private readonly Dictionary<string, object> _variables = new(StringComparer.Ordinal);
    private readonly DuckDBConnection _connection;


    public DuckDBKernel(string name, string connectionString) : this(name, new DuckDBConnection(connectionString))
    {
    }

    public DuckDBKernel(string name, DuckDBConnection connection) : base(name)
    {
        KernelInfo.LanguageName = "KQL";
        _connection = connection;
        RegisterForDisposal(_connection);
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

    async Task IKernelCommandHandler<SubmitCode>.HandleAsync(SubmitCode submitCode, KernelInvocationContext context)
    {
        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync();
        }
        await using var dbCommand = _connection.CreateCommand();

        dbCommand.CommandText = submitCode.Code;

        _tables = Execute(dbCommand);
        var results = new List<TabularDataResource>();

        foreach (var table in _tables)
        {
            var tabularDataResource = table.ToTabularDataResource();

            var explorer = DataExplorer.CreateDefault(tabularDataResource);
            context.Display(explorer);
            results.Add(tabularDataResource);
           
        }

        if (submitCode.Parameters.TryGetValue("--name", out var queryName))
        {
            StoreQueryResults(results, queryName);
        }
    }

    private void StoreQueryResults(IReadOnlyCollection<TabularDataResource> results, string? variableName)
    {
    
        if (!string.IsNullOrWhiteSpace(variableName))
        {
            _resultSets[variableName] = results;
        }
    }

    private IEnumerable<IEnumerable<IEnumerable<(string name, object value)>>> Execute(IDbCommand command)
    {
        using var reader = command.ExecuteReader();

        do
        {
            var values = new object[reader.FieldCount];
            var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();

            AliasDuplicateColumnNames(names);

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
    }

    private static void AliasDuplicateColumnNames(IList<string> columnNames)
    {
        var nameCounts = new Dictionary<string, int>(capacity: columnNames.Count);
        for (var i = 0; i < columnNames.Count; i++)
        {
            var columnName = columnNames[i];
            if (nameCounts.TryGetValue(columnName, out var count))
            {
                nameCounts[columnName] = ++count;
                columnNames[i] = columnName + $" ({count})";
            }
            else
            {
                nameCounts[columnName] = 1;
            }
        }
    }

    public bool TryGetValue<T>(string name, out T? value)
    {
        if (_resultSets.TryGetValue(name, out var resultSet) &&
            resultSet is T resultSetT)
        {
            value = resultSetT;
            return true;
        }
        if (_variables.TryGetValue(name, out var variable) && variable is T variableValue)
        {
            value = variableValue;
            return true;
        }
        value = default;
        return false;
    }

    Task IKernelCommandHandler<RequestValue>.HandleAsync(RequestValue command, KernelInvocationContext context)
    {
        if (TryGetValue<object>(command.Name, out var value))
        {
            context.PublishValueProduced(command, value);
        }
        else
        {
            context.Fail(command, message: $"Value '{command.Name}' not found in kernel {Name}");
        }

        return Task.CompletedTask;
    }

    Task IKernelCommandHandler<RequestValueInfos>.HandleAsync(RequestValueInfos command, KernelInvocationContext context)
    {
        var valueInfos = CreateKernelValueInfos(_variables, command.MimeType).Concat(CreateKernelValueInfos(_resultSets, command.MimeType)).ToArray();

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