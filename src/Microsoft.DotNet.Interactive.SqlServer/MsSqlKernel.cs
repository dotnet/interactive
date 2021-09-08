// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.SqlServer
{
    internal class MsSqlKernel : ToolsServiceKernel
    {
        private readonly string _connectionString;

        internal MsSqlKernel(
            string name,
            string connectionString,
            ToolsServiceClient client) : base(name, client)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));
            }
            
            _connectionString = connectionString;
        }

        public override async Task ConnectAsync()
        {
            if (!Connected)
            {
                await ServiceClient.ConnectAsync(TempFileUri, _connectionString);
                await ConnectionCompleted.Task;
                Connected = true;
            }
        }

        protected override Type GetType(string typeName)
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

            // If a query handler is already defined, then it means another query is already running in parallel.
            // We only want to run one query at a time, so we display an error here instead.
            if (_queryCompletionHandler is not null)
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

                            if (resultSummary.RowCount > 0)
                            {
                                var subsetParams = new QueryExecuteSubsetParams
                                {
                                    OwnerUri = _tempFileUri.AbsolutePath,
                                    BatchIndex = batchSummary.Id,
                                    ResultSetIndex = resultSummary.Id,
                                    RowsStartIndex = 0,
                                    RowsCount = Convert.ToInt32(resultSummary.RowCount)
                                };
                                var subsetResult = await _serviceClient.ExecuteQueryExecuteSubsetAsync(subsetParams);
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
                await _serviceClient.ExecuteQueryStringAsync(_tempFileUri, command.Code);

                context.CancellationToken.Register(() =>
                {

                    _serviceClient.CancelQueryExecutionAsync(_tempFileUri)
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

        private static IEnumerable<IEnumerable<IEnumerable<(string name, object value)>>> GetEnumerableTables(ColumnInfo[] columnInfos, CellValue[][] rows)
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

                        var expectedType = Type.GetType(columnInfo.DataType);

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

            private Option<string> MimeTypeOption { get; } = new(
                "--mime-type",
                description: "Specify the MIME type to use for the data.",
                getDefaultValue: () => HtmlFormatter.MimeType);

            protected override async Task Handle(KernelInvocationContext kernelInvocationContext, InvocationContext commandLineInvocationContext)
            {
                await base.Handle(kernelInvocationContext, commandLineInvocationContext);

                switch (kernelInvocationContext.Command)
                {
                    case SubmitCode c:
                        var mimeType = commandLineInvocationContext.ParseResult.ValueForOption(MimeTypeOption);

                        c.Properties.Add("mime-type", mimeType);
                        break;
                }
            }
        }
    }
}