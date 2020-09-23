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

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class MsSqlKernel :
        Kernel,
        IKernelCommandHandler<SubmitCode>,
        IKernelCommandHandler<RequestCompletions>,
        IAsyncDisposable
    {
        private bool _connected = false;
        private readonly string _connectionUri;
        private readonly string _connectionString;
        private readonly MsSqlServiceClient _serviceClient;

        private TaskCompletionSource<ConnectionCompleteParams> _connectionCompleted = new TaskCompletionSource<ConnectionCompleteParams>();

        private ConcurrentDictionary<string, Action<QueryCompleteParams>> _queryHandlers = new ConcurrentDictionary<string, Action<QueryCompleteParams>>();

        public MsSqlKernel(string name, string connectionString) : base(name)
        {
            _connectionUri = $"connection:{Guid.NewGuid()}";
            _connectionString = connectionString;
            _serviceClient = new MsSqlServiceClient();

            _serviceClient.OnConnectionComplete += HandleConnectionComplete;
            _serviceClient.OnQueryComplete += HandleQueryComplete;

            _serviceClient.StartProcessAndRedirectIO();
        }

        private void HandleConnectionComplete(object sender, ConnectionCompleteParams connParams)
        {
            if (connParams.OwnerUri.Equals(_connectionUri))
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
            Action<QueryCompleteParams> handler;
            if (_queryHandlers.TryGetValue(queryParams.OwnerUri, out handler))
            {
                handler(queryParams);
            }
        }

        public ValueTask DisposeAsync()
        {
            _serviceClient.OnConnectionComplete -= HandleConnectionComplete;
            Task disposeTask;
            if (_connected)
            {
                disposeTask = _serviceClient.DisconnectAsync(_connectionUri);
            }
            else
            {
                disposeTask = Task.CompletedTask;
            }
            return new ValueTask(disposeTask);
        }

        public async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            if (!_connected)
            {
                await _serviceClient.ConnectAsync(_connectionUri, _connectionString);
                await _connectionCompleted.Task;
                _connected = true;
            }

            var queryResult = await _serviceClient.ExecuteSimpleQueryAsync(_connectionUri, command.Code);
            var tableString = GetTableStringForSimpleResult(queryResult);
            await context.DisplayAsync(tableString, HtmlFormatter.MimeType);
        }

        private TabularJsonString GetTableStringForSimpleResult(SimpleExecuteResult executeResult)
        {
            var data = new JArray();
            var columnNames = executeResult.ColumnInfo.Select(info => info.ColumnName).ToArray();
            foreach (CellValue[] cellRow in executeResult.Rows)
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

            var fields = executeResult.ColumnInfo.ToDictionary(column => column.ColumnName, column => Type.GetType(column.DataType));
            return TabularJsonString.Create(fields, data);
        }

        public Task HandleAsync(RequestCompletions command, KernelInvocationContext context)
        {
            throw new NotImplementedException();
        }
    }
}