// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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

            rpc.StartListening();
        }

        public event EventHandler<ConnectionCompleteParams> OnConnectionComplete;
        public event EventHandler<QueryCompleteParams> OnQueryComplete;

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

        public async Task<CompletionItem[]> ProvideCompletionItemsAsync(string code, int linePosition, int charPosition)
        {
            var tempFileName = Path.GetTempFileName();
            var tempFileStream = File.Create(tempFileName, 4096, FileOptions.DeleteOnClose);

            using var writer = new StreamWriter(tempFileStream);
            writer.WriteLine(code);

            TextDocumentIdentifier docId = new TextDocumentIdentifier() { Uri = tempFileName };
            Position position = new Position() { Line = linePosition, Character = charPosition };
            CompletionContext context = new CompletionContext() { TriggerKind = 1 };
            var completionParams = new CompletionParams() { TextDocument = docId, Position = position, Context = context };
            return await rpc.InvokeWithParameterObjectAsync<CompletionItem[]>("textDocument/completion", completionParams);
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

        public void Dispose()
        {
            rpc.Dispose();
            process.Dispose();
        }
    }
}
