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

            var processedResults = ProcessResults(queryResult);

            await context.DisplayAsync(processedResults);
        }

        private List<string[]> ProcessResults(SimpleExecuteResult result)
        {
            var resultTable = new List<string[]>();

            var columnNames = result.ColumnInfo.Select(info => info.ColumnName).ToArray();
            resultTable.Add(columnNames);

            foreach (CellValue[] cellRow in result.Rows)
            {
                var stringRow = cellRow.Select(row => row.DisplayValue).ToArray();
                resultTable.Add(stringRow);
            }

            return resultTable;
        }

        public Task HandleAsync(RequestCompletions command, KernelInvocationContext context)
        {
            throw new NotImplementedException();
        }
    }
}