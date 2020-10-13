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

        private ConcurrentDictionary<string, Action<QueryCompleteParams>> _queryHandlers = new ConcurrentDictionary<string, Action<QueryCompleteParams>>();

        public MsSqlKernel(string name, string connectionString) : base(name)
        {
            _tempFilePath = Path.GetTempFileName();
            _tempFileUri = MsSqlServiceClient.GetUriForFilePath(_tempFilePath);
            _connectionString = connectionString;
            _serviceClient = new MsSqlServiceClient();

            _serviceClient.OnConnectionComplete += HandleConnectionComplete;
            _serviceClient.OnQueryComplete += HandleQueryComplete;
            _serviceClient.OnIntellisenseReady += HandleIntellisenseReady;

            _serviceClient.StartProcessAndRedirectIO();

            RegisterForDisposal(_serviceClient);
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

        private void HandleQueryComplete(object sender, QueryCompleteParams queryParams)
        {
            Action<QueryCompleteParams> handler;
            if (_queryHandlers.TryGetValue(queryParams.OwnerUri, out handler))
            {
                handler(queryParams);
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

            var queryResult = await _serviceClient.ExecuteSimpleQueryAsync(_tempFileUri, command.Code);
            var tableString = GetTableStringForSimpleResult(queryResult);
            context.Display(tableString, HtmlFormatter.MimeType);
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