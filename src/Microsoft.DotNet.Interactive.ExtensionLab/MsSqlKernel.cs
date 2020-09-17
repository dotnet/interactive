// // Copyright (c) .NET Foundation and contributors. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private readonly MsSqlServiceClient serviceClient;

        public MsSqlKernel(string name, string connectionString) : base(name)
        {
            _connectionUri = $"connection:{Guid.NewGuid()}";
            _connectionString = connectionString;
            serviceClient = new MsSqlServiceClient();
            serviceClient.StartProcessAndRedirectIO();
        }

        public ValueTask DisposeAsync()
        {
            if (_connected)
            {
                return new ValueTask(serviceClient.DisconnectAsync(_connectionUri));
            }

            return new ValueTask(Task.CompletedTask);
        }

        public async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            if (!_connected)
            {
                var connectResult = await serviceClient.ConnectAsync(_connectionUri, _connectionString);
                if (!connectResult)
                {
                    await context.DisplayAsync("Failed to connect to database.");
                    return;
                }
                _connected = true;
            }

            Thread.Sleep(10000);

            var queryResult = await serviceClient.ExecuteQueryStringAsync(_connectionUri, command.Code);
            var tableString = GetTableStringForResult(queryResult);
            await context.DisplayAsync(tableString, HtmlFormatter.MimeType);
        }

        private TabularJsonString GetTableStringForResult(SimpleExecuteResult result)
        {
            var data = new JArray();
            var columnNames = result.ColumnInfo.Select(info => info.ColumnName).ToArray();
            foreach (CellValue[] cellRow in result.Rows)
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

            var fields = result.ColumnInfo.ToDictionary(column => column.ColumnName, column => Type.GetType(column.DataType));
            return TabularJsonString.Create(fields, data);
        }

        public Task HandleAsync(RequestCompletions command, KernelInvocationContext context)
        {
            throw new NotImplementedException();
        }
    }
}