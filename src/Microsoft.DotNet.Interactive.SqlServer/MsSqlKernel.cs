// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.ExtensionLab;

namespace Microsoft.DotNet.Interactive.SqlServer
{
    public class MsSqlKernel :
        Kernel,
        IKernelCommandHandler<SubmitCode>,
        IKernelCommandHandler<RequestCompletions>
    {
        private bool _connected = false;
        private bool _intellisenseReady = false;
        private readonly Uri _tempFileUri;
        private readonly string _connectionString;
        private readonly MsSqlServiceClient _serviceClient;

        private readonly TaskCompletionSource<ConnectionCompleteParams> _connectionCompleted = new TaskCompletionSource<ConnectionCompleteParams>();

        private Func<QueryCompleteParams, Task> _queryCompletionHandler = null;
        private Func<MessageParams, Task> _queryMessageHandler = null;

        public MsSqlKernel(string pathToService, string name, string connectionString) : base(name)
        {
             var filePath = Path.GetTempFileName();
            _tempFileUri = new Uri(filePath);
            _connectionString = connectionString;

            _serviceClient = new MsSqlServiceClient(pathToService);
            _serviceClient.Initialize();

            _serviceClient.OnConnectionComplete += HandleConnectionComplete;
            _serviceClient.OnQueryComplete += HandleQueryComplete;
            _serviceClient.OnIntellisenseReady += HandleIntellisenseReady;
            _serviceClient.OnQueryMessage += HandleQueryMessage;

            RegisterForDisposal(() =>
            {
                if (_connected)
                {
                    Task.Run(() => _serviceClient.DisconnectAsync(_tempFileUri)).Wait();
                }
            });
            RegisterForDisposal(() => File.Delete(_tempFileUri.LocalPath));
        }

        private void HandleConnectionComplete(object sender, ConnectionCompleteParams connParams)
        {
            if (connParams.OwnerUri.Equals(_tempFileUri.ToString()))
            {
                if (connParams.ErrorMessage != null)
                {
                    _connectionCompleted.SetException(new Exception(connParams.ErrorMessage));
                }
                else
                {
                    _connectionCompleted.SetResult(connParams);
                }
            }
        }

        private void HandleQueryComplete(object sender, QueryCompleteParams queryParams)
        {
            if (_queryCompletionHandler != null)
            {
                Task.Run(() => _queryCompletionHandler(queryParams)).Wait();
            }
        }

        private void HandleQueryMessage(object sender, MessageParams messageParams)
        {
            if (_queryMessageHandler != null)
            {
                Task.Run(() => _queryMessageHandler(messageParams)).Wait();
            }
        }

        private void HandleIntellisenseReady(object sender, IntelliSenseReadyParams readyParams)
        {
            if (readyParams.OwnerUri.Equals(_tempFileUri.ToString()))
            {
                _intellisenseReady = true;
            }
        }

        public async Task ConnectAsync()
        {
            if (!_connected)
            {
                await _serviceClient.ConnectAsync(_tempFileUri, _connectionString);
                await _connectionCompleted.Task;
                _connected = true;
            }
        }

        public async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            if (!_connected)
            {
                return;
            }

            if (_queryCompletionHandler != null)
            {
                context.Display("Error: Another query is currently running. Please wait for that query to complete before re-running this cell.");
                return;
            }

            var completion = new TaskCompletionSource<bool>();

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

                            var subsetParams = new QueryExecuteSubsetParams()
                            {
                                OwnerUri = _tempFileUri.ToString(),
                                BatchIndex = batchSummary.Id,
                                ResultSetIndex = resultSummary.Id,
                                RowsStartIndex = 0,
                                RowsCount = Convert.ToInt32(resultSummary.RowCount)
                            };
                            var subsetResult = await _serviceClient.ExecuteQueryExecuteSubsetAsync(subsetParams);

                            if (subsetResult.Message != null)
                            {
                                context.Fail(message: subsetResult.Message);
                            }
                            else
                            {
                                var table = GetEnumerableTable(resultSummary.ColumnInfo, subsetResult.ResultSubset.Rows);
                                context.Display(table);
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
                        context.Fail(message: messageParams.Message.Message);
                        completion.SetResult(true);
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
                var queryResult = await _serviceClient.ExecuteQueryStringAsync(_tempFileUri, command.Code);
                await completion.Task;
            }
            finally
            {
                _queryCompletionHandler = null;
                _queryMessageHandler = null;
            }
        }

        private IEnumerable<IEnumerable<IEnumerable<(string, object)>>> GetEnumerableTable(ColumnInfo[] columnInfo, CellValue[][] rows)
        {
            var displayTable = new List<(string, object)[]>();
            var columnNames = columnInfo.Select(info => info.ColumnName).ToArray();

            SqlKernelUtils.AliasDuplicateColumnNames(columnNames);

            foreach (CellValue[] row in rows)
            {
                var displayRow = new (string, object)[row.Length];
                for (int i = 0; i < row.Length; i++)
                {
                    displayRow[i] = (columnNames[i], row[i].DisplayValue);
                }
                displayTable.Add(displayRow);
            }
            yield return displayTable;
        }

        public async Task HandleAsync(RequestCompletions command, KernelInvocationContext context)
        {
            if (!_intellisenseReady)
            {
                return;
            }
            var completionItems = await _serviceClient.ProvideCompletionItemsAsync(_tempFileUri, command);
            context.Publish(new CompletionsProduced(completionItems, command));
        }
    }
}