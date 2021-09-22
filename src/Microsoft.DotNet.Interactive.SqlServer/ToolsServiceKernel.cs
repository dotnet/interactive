﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.ExtensionLab;
using Microsoft.DotNet.Interactive.Formatting.TabularData;

namespace Microsoft.DotNet.Interactive.SqlServer
{
    internal abstract class ToolsServiceKernel : 
        Kernel,
        IKernelCommandHandler<SubmitCode>,
        IKernelCommandHandler<RequestCompletions>
    {
        protected readonly Uri TempFileUri;
        protected readonly TaskCompletionSource<ConnectionCompleteParams> ConnectionCompleted = new();
        private Func<QueryCompleteParams, Task> _queryCompletionHandler;
        private Func<MessageParams, Task> _queryMessageHandler;
        private bool _intellisenseReady;
        protected bool Connected;
        protected readonly ToolsServiceClient ServiceClient;

        protected ToolsServiceKernel(string name, ToolsServiceClient client) : base(name)
        {
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

        public async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
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
                                var subsetResult = await ServiceClient.ExecuteQueryExecuteSubsetAsync(subsetParams);
                                var tables = GetEnumerableTables(resultSummary.ColumnInfo, subsetResult.ResultSubset.Rows);
                                foreach (var table in tables)
                                {
                                    var explorer = new NteractDataExplorer(table.ToTabularDataResource());
                                    context.Display(explorer);
                                }
                            }
                            else
                            {
                                context.Display($"Info: No rows were returned for query {resultSummary.Id} in batch {batchSummary.Id}.");
                            }
                        }
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
                await ServiceClient.ExecuteQueryStringAsync(TempFileUri, command.Code);

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
        
        private IEnumerable<IEnumerable<IEnumerable<(string name, object value)>>> GetEnumerableTables(ColumnInfo[] columnInfos, CellValue[][] rows)
        {
            var displayTable = new List<(string, object)[]>();
            var columnNames = columnInfos.Select(info => info.ColumnName).ToArray();

            SqlKernelUtils.AliasDuplicateColumnNames(columnNames);

            foreach (CellValue[] row in rows)
            {
                var displayRow = new (string, object)[row.Length];

                for (var colIndex = 0; colIndex < row.Length; colIndex++)
                {
                    object convertedValue = default;

                    try
                    {
                        var columnInfo = columnInfos[colIndex];

                        var expectedType = GetType(columnInfo.DataType);

                        if (TypeDescriptor.GetConverter(expectedType) is { } typeConverter)
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
                    catch (Exception)
                    {
                        convertedValue = row[colIndex].DisplayValue;
                    }

                    displayRow[colIndex] = (columnNames[colIndex], convertedValue);
                }

                displayTable.Add(displayRow);
            }

            yield return displayTable;
        }
        
        protected abstract Type GetType(string typeName);

        public async Task HandleAsync(RequestCompletions command, KernelInvocationContext context)
        {
            if (!_intellisenseReady)
            {
                return;
            }

            var completionItems = await ServiceClient.ProvideCompletionItemsAsync(TempFileUri, command);
            context.Publish(new CompletionsProduced(completionItems, command));
        }
    }
}