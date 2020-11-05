// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using System.Data;
using Microsoft.DotNet.Interactive.Formatting;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.ExtensionLab
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

        private TaskCompletionSource<ConnectionCompleteParams> _connectionCompleted = new TaskCompletionSource<ConnectionCompleteParams>();

        private Func<QueryCompleteParams, Task> _queryCompletionHandler = null;

        public MsSqlKernel(string name, string connectionString) : base(name)
        {
            var filePath = Path.GetTempFileName();
            _tempFileUri = new Uri(filePath);
            _connectionString = connectionString;

            _serviceClient = MsSqlServiceClient.Instance;
            _serviceClient.Initialize();

            _serviceClient.OnConnectionComplete += HandleConnectionComplete;
            _serviceClient.OnQueryComplete += HandleQueryCompleteAsync;
            _serviceClient.OnIntellisenseReady += HandleIntellisenseReady;

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

        private void HandleQueryCompleteAsync(object sender, QueryCompleteParams queryParams)
        {
            if (_queryCompletionHandler != null)
            {
                Task.Run(() => _queryCompletionHandler(queryParams)).Wait();
            }
        }

        private void HandleIntellisenseReady(object sender, IntelliSenseReadyParams readyParams)
        {
            if (readyParams.OwnerUri.Equals(this._tempFileUri.ToString()))
            {
                this._intellisenseReady = true;
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
                                context.Display(subsetResult.Message);
                            }
                            else
                            {
                                var tableString = GetTableStringForResult(resultSummary.ColumnInfo, subsetResult.ResultSubset.Rows);
                                context.Display(tableString);
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

            try
            {
                var queryResult = await _serviceClient.ExecuteQueryStringAsync(_tempFileUri, command.Code);
                await completion.Task;
            }
            finally
            {
                _queryCompletionHandler = null;
            }
        }

        private TabularJsonString GetTableStringForResult(ColumnInfo[] columnInfo, CellValue[][] rows)
        {
            var data = new JArray();
            var columnNames = columnInfo.Select(info => info.ColumnName).ToArray();
            foreach (CellValue[] cellRow in rows)
            {
                var rowObj = new JObject();
                for (int i = 0; i < cellRow.Length; i++)
                {
                    var cell = cellRow[i];
                    var fromObject = JToken.FromObject(cell.DisplayValue);
                    rowObj.Add(columnNames[i], fromObject);
                }
                data.Add(rowObj);
            }

            var fields = columnInfo.ToDictionary(column => column.ColumnName, column => Type.GetType(column.DataType));
            return TabularJsonString.Create(fields, data);
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