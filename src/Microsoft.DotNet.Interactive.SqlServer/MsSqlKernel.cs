// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.ExtensionLab;
using Microsoft.DotNet.Interactive.Formatting;

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

        public MsSqlKernel(
            string name,
            string connectionString,
            MsSqlServiceClient client) : base(name)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));
            }

            var filePath = Path.GetTempFileName();
            _tempFileUri = new Uri(filePath);
            _connectionString = connectionString;

            _serviceClient = client ?? throw new ArgumentNullException(nameof(client));
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
            if (connParams.OwnerUri.Equals(_tempFileUri.AbsolutePath))
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
            if (readyParams.OwnerUri.Equals(_tempFileUri.AbsolutePath))
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

                            CellValue[][] rows = null;
                            if (resultSummary.RowCount > 0)
                            {
                                var subsetParams = new QueryExecuteSubsetParams
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
                                    continue;
                                }
                                else
                                {
                                    rows = subsetResult.ResultSubset.Rows;
                                }
                            }

                            var tables = GetEnumerableTables(resultSummary.ColumnInfo, rows);
                            context.Display(tables);
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
                await _serviceClient.ExecuteQueryStringAsync(_tempFileUri, command.Code);
                await completion.Task;
            }
            finally
            {
                _queryCompletionHandler = null;
                _queryMessageHandler = null;
            }
        }

        public static IEnumerable<IEnumerable<IEnumerable<(string name, object value)>>> GetEnumerableTables(ColumnInfo[] columnInfos, CellValue[][] rows)
        {
            var displayTable = new List<(string, object)[]>();
            var columnNames = columnInfos.Select(info => info.ColumnName).ToArray();

            SqlKernelUtils.AliasDuplicateColumnNames(columnNames);

            if (rows == null || rows.Length == 0)
            {
                // If there are no rows, then add an empty row so that we at least display column names
                var displayRow = new (string, object)[columnNames.Length];
                for (int i = 0; i < columnNames.Length; i++)
                {
                    displayRow[i] = (columnNames[i], null);
                }
                displayTable.Add(displayRow);
            }
            else
            {
                foreach (CellValue[] row in rows)
                {
                    var displayRow = new (string, object)[row.Length];

                    for (var colIndex = 0; colIndex < row.Length; colIndex++)
                    {
                        object convertedValue = default;

                        try
                        {
                            var columnInfo = columnInfos[colIndex];

                            var expectedType = Type.GetType(columnInfo.DataType);

                            if (TypeDescriptor.GetConverter(expectedType) is { } typeConverter)
                            {
                                if (typeConverter.CanConvertFrom(typeof(string)))
                                {
                                    convertedValue = typeConverter.ConvertFromInvariantString(row[colIndex].DisplayValue);
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

        protected override ChooseKernelDirective CreateChooseKernelDirective() => 
            new ChooseMsSqlKernelDirective(this);

        private class ChooseMsSqlKernelDirective : ChooseKernelDirective
        {
            public ChooseMsSqlKernelDirective(Kernel kernel) : base(kernel, $"Run a T-SQL query using the \"{kernel.Name}\" connection.")
            {
                Add(MimeTypeOption);
            }

            private Option<string> MimeTypeOption { get; } = new Option<string>(
                "--mime-type",
                description: "Specify the MIME type to use for the data.",
                getDefaultValue: () => HtmlFormatter.MimeType);

            protected override async Task Handle(KernelInvocationContext kernelInvocationContext, InvocationContext commandLineInvocationContext)
            {
                await base.Handle(kernelInvocationContext, commandLineInvocationContext);

                switch (kernelInvocationContext.Command)
                {
                    case SubmitCode c:
                        var mimeType = commandLineInvocationContext.ParseResult.FindResultFor(MimeTypeOption)?.GetValueOrDefault();

                        c.Properties.Add("mime-type", mimeType);
                        break;
                }
            }
        }
    }
}