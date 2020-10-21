// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using System.Data;
using System.Reactive.Disposables;
using System.Threading;
using Microsoft.DotNet.Interactive.Formatting;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class MsSqlKernel :
        Kernel,
        IKernelCommandHandler<SubmitCode>,
        IKernelCommandHandler<RequestCompletions>,
        IKernelCommandHandler<RequestHoverText>
    {
        private bool _connected = false;
        private bool _intellisenseReady = false;
        private readonly string _tempFilePath;
        private readonly string _tempFileUri;
        private readonly string _connectionString;
        private readonly MsSqlServiceClient _serviceClient;

        private TaskCompletionSource<ConnectionCompleteParams> _connectionCompleted = new TaskCompletionSource<ConnectionCompleteParams>();

        private Dictionary<string, Func<QueryCompleteParams, Task>> _queryHandlers = new Dictionary<string, Func<QueryCompleteParams, Task>>();

        public MsSqlKernel(string name, string connectionString) : base(name)
        {
            _tempFilePath = Path.GetTempFileName();
            _tempFileUri = MsSqlServiceClient.GetUriForFilePath(_tempFilePath);
            _connectionString = connectionString;
            _serviceClient = new MsSqlServiceClient();

            _serviceClient.OnConnectionComplete += HandleConnectionComplete;
            _serviceClient.OnQueryComplete += HandleQueryCompleteAsync;
            _serviceClient.OnIntellisenseReady += HandleIntellisenseReady;

            _serviceClient.StartProcessAndRedirectIO();

            RegisterForDisposal(_serviceClient);
            RegisterForDisposal(() => File.Delete(_tempFilePath));
        }

        private void HandleConnectionComplete(object sender, ConnectionCompleteParams connParams)
        {
            if (connParams.OwnerUri.Equals(_tempFileUri))
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

        private async void HandleQueryCompleteAsync(object sender, QueryCompleteParams queryParams)
        {
            Func<QueryCompleteParams, Task> handler;
            if (_queryHandlers.TryGetValue(queryParams.OwnerUri, out handler))
            {
                await handler(queryParams);
            }
        }

        private void HandleIntellisenseReady(object sender, IntelliSenseReadyParams readyParams)
        {
            if (readyParams.OwnerUri.Equals(this._tempFileUri))
            {
                this._intellisenseReady = true;
            }
        }

        public async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            if (!_connected)
            {
                await _serviceClient.ConnectAsync(_tempFileUri, _connectionString);
                await _connectionCompleted.Task;
                _connected = true;
            }

            var completion = new TaskCompletionSource<bool>();
            Func<QueryCompleteParams, Task> handler = async queryParams =>
            {
                foreach (var batchSummary in queryParams.BatchSummaries)
                {
                    foreach (var resultSummary in batchSummary.ResultSetSummaries)
                    {
                        var subsetParams = new QueryExecuteSubsetParams()
                        {
                            OwnerUri = _tempFileUri,
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
            };
            _queryHandlers.Add(_tempFileUri, handler);

            try
            {
                var queryResult = await _serviceClient.ExecuteQueryStringAsync(_tempFileUri, command.Code);
            }
            catch
            {
                _queryHandlers.Remove(_tempFileUri);
                throw;
            }

            await completion.Task;
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
            var completionItems = await _serviceClient.ProvideCompletionItemsAsync(_tempFilePath, command);
            context.Publish(new CompletionsProduced(completionItems, command));
        }

        public async Task HandleAsync(RequestHoverText command, KernelInvocationContext context)
        {
            if (!_intellisenseReady)
            {
                return;
            }
            var hoverItem = await _serviceClient.ProvideHoverAsync(_tempFilePath, command);
            if (hoverItem != null)
            {
                var stringBuilder = new StringBuilder();
                foreach (var markedString in hoverItem.Contents)
                {
                    stringBuilder.AppendLine(markedString.Value);
                }

                context.Publish(new HoverTextProduced(
                    command,
                    new[]
                    {
                        new FormattedValue("text/markdown", stringBuilder.ToString())
                    }));
            }
        }
    }
}