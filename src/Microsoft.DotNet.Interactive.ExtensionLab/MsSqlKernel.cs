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
        private readonly string _queryUri;
        private readonly string _connectionString;
        private readonly MsSqlServiceClient serviceClient;

        public MsSqlKernel(string name, string connectionString) : base(name)
        {
            _connectionUri = $"connection:{Guid.NewGuid()}";
            _queryUri = $"untitled:{Guid.NewGuid()}";
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

            var queryResult = await serviceClient.ExecuteQueryStringAsync(_connectionUri, command.Code);

            var processedResults = await context.DisplayAsync(queryResult);

            await context.DisplayAsync(processedResults);
        }

        private IEnumerable<IEnumerable<IEnumerable<(string name, object value)>>> Execute(SimpleExecuteResult result)
        {
            var values = new object[result.ColumnInfo.Length];
            var columnNames = Enumerable.Range(0, result.ColumnInfo.Length).Select(i => result.ColumnInfo[i].ColumnName).ToArray();

            // holds the result of a single statement within the query
            var resultTable = new List<(string, object)[]>();

            foreach (DbCellValue[] cellRow in result.Rows)
            {
                var resultRow = new (string, object)[cellRow.Length];
                for (var i = 0; i < cellRow.Length; i++)
                {
                    resultRow[i] = (columnNames[i], cellRow[i]);
                }

                resultTable.Add(resultRow);
            }

            yield return resultTable;
        }

        public Task HandleAsync(RequestCompletions command, KernelInvocationContext context)
        {
            throw new NotImplementedException();
        }
    }
}