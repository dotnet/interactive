// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.ValueSharing;

namespace Microsoft.DotNet.Interactive.SqlServer;

public abstract class ToolsServiceKernel :
    Kernel,
    IKernelCommandHandler<SubmitCode>,
    IKernelCommandHandler<RequestCompletions>,
    IKernelCommandHandler<RequestValueInfos>,
    IKernelCommandHandler<RequestValue>,
    IKernelCommandHandler<SendValue>
{
    protected readonly Uri TempFileUri;
    protected readonly TaskCompletionSource<ConnectionCompleteParams> ConnectionCompleted = new();
    private Func<QueryCompleteParams, Task> _queryCompletionHandler;
    private Func<MessageParams, Task> _queryMessageHandler;
    private bool _intellisenseReady;
    protected bool Connected;
    protected readonly ToolsServiceClient ServiceClient;
    private readonly Dictionary<string, object> _variables  = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object> _resultSets = new(StringComparer.Ordinal);


    protected ToolsServiceKernel(string name, ToolsServiceClient client, string languageName) : base(name)
    {
        KernelInfo.LanguageName = languageName;
        var filePath = Path.GetTempFileName();
        TempFileUri = new Uri(filePath);

        ServiceClient = client ?? throw new ArgumentNullException(nameof(client));
        ServiceClient.Initialize();

        ServiceClient.OnConnectionComplete += HandleConnectionComplete;
        ServiceClient.OnQueryComplete += HandleQueryComplete;
        ServiceClient.OnIntellisenseReady += HandleIntellisenseReady;
        ServiceClient.OnQueryMessage += HandleQueryMessage;

        RegisterForDisposal(() =>
        {
            if (Connected)
            {
                Task.Run(() => ServiceClient.DisconnectAsync(TempFileUri)).Wait();
            }
        });
        RegisterForDisposal(() => File.Delete(TempFileUri.LocalPath));
    }

    private void HandleConnectionComplete(object sender, ConnectionCompleteParams connParams)
    {
        if (connParams.OwnerUri.Equals(TempFileUri.AbsolutePath))
        {
            if (connParams.ErrorMessage is not null)
            {
                ConnectionCompleted.SetException(new Exception(connParams.ErrorMessage));
            }
            else
            {
                ConnectionCompleted.SetResult(connParams);
            }
        }
    }

    private void HandleQueryComplete(object sender, QueryCompleteParams queryParams)
    {
        if (_queryCompletionHandler is not null)
        {
            Task.Run(() => _queryCompletionHandler(queryParams)).Wait();
        }
    }

    private void HandleQueryMessage(object sender, MessageParams messageParams)
    {
        if (_queryMessageHandler is not null)
        {
            Task.Run(() => _queryMessageHandler(messageParams)).Wait();
        }
    }

    private void HandleIntellisenseReady(object sender, IntelliSenseReadyParams readyParams)
    {
        if (readyParams.OwnerUri.Equals(TempFileUri.AbsolutePath))
        {
            _intellisenseReady = true;
        }
    }

    public abstract Task ConnectAsync();

    async Task IKernelCommandHandler<SubmitCode>.HandleAsync(SubmitCode command, KernelInvocationContext context)
    {
        if (!Connected)
        {
            return;
        }

        // If a query handler is already defined, then it means another query is already running in parallel.
        // We only want to run one query at a time, so we display an error here instead.
        if (_queryCompletionHandler is not null)
        {
            context.Display("Error: Another query is currently running. Please wait for that query to complete before re-running this cell.");
            return;
        }

        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        _queryCompletionHandler = async queryParams =>
        {
            try
            {
                var results = new List<TabularDataResource>();
                try
                {
                    foreach (var batchSummary in queryParams.BatchSummaries)
                    {
                        foreach (var resultSummary in batchSummary.ResultSetSummaries)
                        {
                            if (completion.Task.IsCompleted)
                            {
                                return;
                            }

                            if (resultSummary.RowCount > 0)
                            {
                                var subsetParams = new QueryExecuteSubsetParams
                                {
                                    OwnerUri = TempFileUri.AbsolutePath,
                                    BatchIndex = batchSummary.Id,
                                    ResultSetIndex = resultSummary.Id,
                                    RowsStartIndex = 0,
                                    RowsCount = Convert.ToInt32(resultSummary.RowCount)
                                };
                                    
                                var subsetResult = await ServiceClient.ExecuteQueryExecuteSubsetAsync(subsetParams, context.CancellationToken);
                                var tabularDataResources = GetTabularDataResources(resultSummary.ColumnInfo, subsetResult.ResultSubset.Rows);

                                foreach (var tabularDataResource in tabularDataResources)
                                {
                                    // Store each result set in the list of result sets being saved

                                    results.Add(tabularDataResource);

                                    var explorer = DataExplorer.CreateDefault(tabularDataResource);
                                    context.Display(explorer);
                                }
                            }
                            else
                            {
                                context.Display($"Info: No rows were returned for query {resultSummary.Id} in batch {batchSummary.Id}.");
                            }
                        }
                    }
                }
                finally
                {
                    // Always store the query results - even if an exception occurred - so we don't end up with stale results
                    // FIX: (HandleAsync) query name
                    StoreQueryResultSet("", results);
                }

                completion.SetResult(true);
            }
            catch (Exception e)
            {
                completion.SetException(e);
            }
        };

#pragma warning disable 1998
        _queryMessageHandler = async messageParams =>
        {
            try
            {
                if (messageParams.Message.IsError)
                {
                    context.Fail(command, message: messageParams.Message.Message);
                    completion.SetResult(true);
                }
                else
                {
                    context.Display(messageParams.Message.Message);
                }
            }
            catch (Exception e)
            {
                completion.SetException(e);
            }
        };
#pragma warning restore 1998

        try
        {
            var query = PrependVariableDeclarationsToCode(command, context);
            await ServiceClient.ExecuteQueryStringAsync(TempFileUri, query, context.CancellationToken);

            context.CancellationToken.Register(() =>
            {

                ServiceClient.CancelQueryExecutionAsync(TempFileUri)
                    .Wait(TimeSpan.FromSeconds(10));

                completion.TrySetCanceled(context.CancellationToken);
            });
            await completion.Task;
        }
        catch (TaskCanceledException)
        {
            context.Display("Query cancelled.");
        }
        catch (OperationCanceledException)
        {
            context.Display("Query cancelled.");
        }
        finally
        {
            _queryCompletionHandler = null;
            _queryMessageHandler = null;
        }
    }

    private static IEnumerable<TabularDataResource> GetTabularDataResources(ColumnInfo[] columnInfos, CellValue[][] rows)
    {
        var schema = new TableSchema();
        var dataRows = new List<List<KeyValuePair<string, object>>>();
        var columnNames = columnInfos.Select(c => c.ColumnName).ToArray();

        ResolveColumnNameClashes(columnNames);

        for (var i = 0; i <  columnInfos.Length; i++)
        {
            var columnInfo = columnInfos[i];
            var columnName = columnNames[i];

            var expectedType = Type.GetType(columnInfo.DataType);
            schema.Fields.Add(new TableSchemaFieldDescriptor(columnName, expectedType.ToTableSchemaFieldType()));
            if (columnInfo.IsKey == true)
            {
                schema.PrimaryKey.Add(columnName);
            }
        }
            
        foreach (var row in rows)
        {
            var dataRow = new List<KeyValuePair<string,object>>();

            for (var colIndex = 0; colIndex < row.Length; colIndex++)
            {
                object convertedValue = default;

                try
                {
                    var columnInfo = columnInfos[colIndex];

                    var expectedType = Type.GetType(columnInfo.DataType);

                    if (TypeDescriptor.GetConverter(expectedType) is { } typeConverter)
                    {
                        if (!row[colIndex].IsNull)
                        {
                            if (typeConverter.CanConvertFrom(typeof(string)))
                            {
                                // TODO:fix handling target boolean type when the column is bit type with numeric value
                                if ((expectedType == typeof(bool) || expectedType == typeof(bool?)) &&

                                    decimal.TryParse(row[colIndex].DisplayValue, out var numericValue))
                                {
                                    convertedValue = numericValue != 0;
                                }
                                else
                                {
                                    convertedValue =
                                        typeConverter.ConvertFromInvariantString(row[colIndex].DisplayValue);
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    convertedValue = row[colIndex].DisplayValue;
                }
                    
                dataRow.Add(new KeyValuePair<string, object>( columnNames[colIndex], convertedValue));
            }
                
            dataRows.Add(dataRow);
        }

        yield return new TabularDataResource(schema, dataRows);

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

    async Task IKernelCommandHandler<RequestCompletions>.HandleAsync(RequestCompletions command, KernelInvocationContext context)
    {
        if (!_intellisenseReady)
        {
            return;
        }

        var completionItems = await ServiceClient.ProvideCompletionItemsAsync(TempFileUri, command);
        context.Publish(new CompletionsProduced(completionItems, command));
    }

    public bool TryGetValue<T>(string name, out T value)
    {
        if (_variables.TryGetValue(name, out var variable) &&
            variable is T variableValue)
        {
            value = variableValue;
            return true;
        }

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

    private string PrependVariableDeclarationsToCode(SubmitCode command, KernelInvocationContext context)
    {
        var sb = new StringBuilder();

        foreach (var variableNameAndValue in _variables)
        {
            var value = variableNameAndValue.Value;

            if (value is PasswordString ps)
            {
                value = ps.GetClearTextPassword();
            }

            var declareStatement = CreateVariableDeclaration(variableNameAndValue.Key, value);

            var displayStatement = declareStatement;

            if (variableNameAndValue.Value is PasswordString pwd)
            {
                displayStatement = displayStatement.Replace(pwd.GetClearTextPassword(), pwd.ToString());
            }

            context.Display($"Adding shared variable declaration statement : {displayStatement}");
            sb.AppendLine(declareStatement);
        }

        sb.AppendLine(command.Code);

        return sb.ToString();
    }

    /// <summary>
    /// Generates the language-specific declaration statement to insert into the code being executed.
    /// </summary>
    protected abstract string CreateVariableDeclaration(string name, object value);

    /// <summary>
    /// Whether the kernel can support turning the specified input variable into some sort of declaration statement.
    /// </summary>
    /// <param name="name">The name of the parameter</param>
    /// <param name="value">The actual parameter value</param>
    /// <param name="msg">The error message to display if the variable isn't supported</param>
    /// <returns></returns>
    protected abstract bool CanDeclareVariable(string name, object value, out string msg);

    async Task IKernelCommandHandler<SendValue>.HandleAsync(
        SendValue command,
        KernelInvocationContext context)
    {
        await SetValueAsync(command, context, (name, value, declaredType) =>
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), $"Sharing null values is not supported at this time.");
            }

            if (value is PasswordString ps)
            {
                value = ps.GetClearTextPassword();
            }

            if (!CanDeclareVariable(name, value, out string msg))
            {
                throw new ArgumentException($"Cannot support value of Type {value.GetType()}. {msg}");
            }

            _variables[name] = value;
            return Task.CompletedTask;
        });
    }

    protected void StoreQueryResultSet(string name, IReadOnlyCollection<TabularDataResource> queryResultSet)
    {
        _resultSets[name] = queryResultSet;
    }
}