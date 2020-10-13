// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using StreamJsonRpc;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class MsSqlServiceClient : IDisposable
    {
        private Process process;
        private JsonRpc rpc;

        public void StartProcessAndRedirectIO()
        {
            var startInfo = new ProcessStartInfo("C:\\SqlToolsService\\MicrosoftSqlToolsServiceLayer.exe")
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            process = new Process
            {
                StartInfo = startInfo
            };
            process.Start();

            rpc = new JsonRpc(process.StandardInput.BaseStream, process.StandardOutput.BaseStream);

            rpc.AddLocalRpcMethod(
                handler: typeof(MsSqlServiceClient).GetMethod(nameof(HandleConnectionCompletion)),
                target: this,
                methodRpcSettings: new JsonRpcMethodAttribute("connection/complete")
                {
                    UseSingleObjectParameterDeserialization = true
                });
            rpc.AddLocalRpcMethod(
                handler: typeof(MsSqlServiceClient).GetMethod(nameof(HandleQueryCompletion)),
                target: this,
                methodRpcSettings: new JsonRpcMethodAttribute("query/complete")
                {
                    UseSingleObjectParameterDeserialization = true
                });
            rpc.AddLocalRpcMethod(
                handler: typeof(MsSqlServiceClient).GetMethod(nameof(HandleIntellisenseReady)),
                target: this,
                methodRpcSettings: new JsonRpcMethodAttribute("textDocument/intelliSenseReady")
                {
                    UseSingleObjectParameterDeserialization = true
                });

            rpc.StartListening();
        }

        public event EventHandler<ConnectionCompleteParams> OnConnectionComplete;
        public event EventHandler<QueryCompleteParams> OnQueryComplete;
        public event EventHandler<IntelliSenseReadyParams> OnIntellisenseReady;

        public async Task<bool> ConnectAsync(string ownerUri, string connectionStr)
        {
            var connectionOptions = new Dictionary<string, string>();
            connectionOptions.Add("ConnectionString", connectionStr);

            var connectionDetails = new ConnectionDetails() { Options = connectionOptions };
            var connectionParams = new ConnectParams() { OwnerUri = ownerUri, Connection = connectionDetails };

            return await rpc.InvokeWithParameterObjectAsync<bool>("connection/connect", connectionParams);
        }

        public async Task<bool> DisconnectAsync(string ownerUri)
        {
            var disconnectParams = new DisconnectParams() { OwnerUri = ownerUri };
            return await rpc.InvokeWithParameterObjectAsync<bool>("connection/disconnect", disconnectParams);
        }

        public async Task<IEnumerable<CompletionItem>> ProvideCompletionItemsAsync(string filePath, RequestCompletions command)
        {
            var tempFileStream = File.Open(filePath, FileMode.OpenOrCreate);
            try
            {
                using (var writer = new StreamWriter(tempFileStream))
                {
                    await writer.WriteLineAsync(command.Code);
                }

                var fileUri = GetUriForFilePath(filePath);
                TextDocumentIdentifier docId = new TextDocumentIdentifier() { Uri = fileUri };
                Position position = new Position() { Line = command.LinePosition.Line, Character = command.LinePosition.Character };
                CompletionContext context = new CompletionContext() { TriggerKind = (int)CompletionTriggerKind.Invoked };
                var completionParams = new CompletionParams() { TextDocument = docId, Position = position, Context = context };
                var sqlCompletionItems = await rpc.InvokeWithParameterObjectAsync<SqlCompletionItem[]>("textDocument/completion", completionParams);

                return sqlCompletionItems.Select(item =>
                {
                    return new CompletionItem(
                        item.Label,
                        item.Kind != null ? Enum.GetName(typeof(SqlCompletionItemKind), item.Kind) : string.Empty,
                        item.FilterText,
                        item.SortText,
                        item.InsertText,
                        item.Documentation);
                });
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        public async Task<Hover> ProvideHoverAsync(string filePath, RequestHoverText command)
        {
            var tempFileStream = File.Open(filePath, FileMode.OpenOrCreate);
            try
            {
                using (var writer = new StreamWriter(tempFileStream))
                {
                    await writer.WriteLineAsync(command.Code);
                }

                var fileUri = GetUriForFilePath(filePath);
                TextDocumentIdentifier docId = new TextDocumentIdentifier() { Uri = fileUri };
                Position position = new Position() { Line = command.LinePosition.Line, Character = command.LinePosition.Character };
                var positionParams = new TextDocumentPositionParams() { TextDocument = docId, Position = position };
                return await rpc.InvokeWithParameterObjectAsync<Hover>("textDocument/hover", positionParams);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        public async Task<ExecuteRequestResult> ExecuteQueryStringAsync(string ownerUri, string queryString)
        {
            var queryParams = new ExecuteStringParams() { OwnerUri = ownerUri, QueryString = queryString };
            return await rpc.InvokeWithParameterObjectAsync<ExecuteRequestResult>("query/executeString", queryParams);
        }

        public async Task<SimpleExecuteResult> ExecuteSimpleQueryAsync(string ownerUri, string queryString)
        {
            var queryParams = new ExecuteStringParams() { OwnerUri = ownerUri, QueryString = queryString };
            return await rpc.InvokeWithParameterObjectAsync<SimpleExecuteResult>("query/simpleexecute", queryParams);
        }

        public async Task<QueryExecuteSubsetResult> ExecuteQueryExecuteSubsetAsync(string ownerUri)
        {
            var queryExecuteSubsetParams = new QueryExecuteSubsetParams() { OwnerUri = ownerUri, ResultSetIndex = 0, RowsCount = 1 };
            return await rpc.InvokeWithParameterObjectAsync<QueryExecuteSubsetResult>("query/subset", queryExecuteSubsetParams);
        }

        public void HandleConnectionCompletion(ConnectionCompleteParams connParams)
        {
            OnConnectionComplete(this, connParams);
        }

        public void HandleQueryCompletion(QueryCompleteParams queryParams)
        {
            OnQueryComplete(this, queryParams);
        }

        public void HandleIntellisenseReady(IntelliSenseReadyParams readyParams)
        {
            OnIntellisenseReady(this, readyParams);
        }

        public void Dispose()
        {
            rpc.Dispose();
            process.Kill(true);
            process.Dispose();
        }

        public static string GetUriForFilePath(string filePath)
        {
            return $"file:///{filePath.Replace('\\', '/')}";
        }
    }
}
