// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using StreamJsonRpc;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class MsSqlServiceClient : IDisposable
    {
        private Process _process;
        private JsonRpc _rpc;
        private bool _initialized = false;
        private readonly string _serviceExePath;

        public const string SqlToolsServiceEnvironmentVariableName = "DOTNET_SQLTOOLSSERVICE";

        public MsSqlServiceClient(string serviceExePath = null)
        {
            _serviceExePath = serviceExePath;
        }

        public void Initialize()
        {
            if (!_initialized)
            {
                StartProcessAndRedirectIO();
                _initialized = true;
            }
        }

        private void StartProcessAndRedirectIO()
        {
            if (_serviceExePath == null)
            {
                throw new InvalidOperationException("Path to SQL Tools Service executable was not provided.");
            }

            var startInfo = new ProcessStartInfo(_serviceExePath)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Arguments = $"--parent-pid {Process.GetCurrentProcess().Id}"
            };
            _process = new Process
            {
                StartInfo = startInfo
            };
            _process.Start();

            _rpc = new JsonRpc(_process.StandardInput.BaseStream, _process.StandardOutput.BaseStream);

            AddLocalRpcMethod(nameof(HandleConnectionCompletion), "connection/complete");
            AddLocalRpcMethod(nameof(HandleQueryCompletion), "query/complete");
            AddLocalRpcMethod(nameof(HandleQueryMessage), "query/message");
            AddLocalRpcMethod(nameof(HandleIntellisenseReady), "textDocument/intelliSenseReady");

            _rpc.StartListening();
        }

        private void AddLocalRpcMethod(string localMethodName, string rpcMethodName)
        {
            _rpc.AddLocalRpcMethod(
                handler: typeof(MsSqlServiceClient).GetMethod(localMethodName),
                target: this,
                methodRpcSettings: new JsonRpcMethodAttribute(rpcMethodName)
                {
                    UseSingleObjectParameterDeserialization = true
                });
        }

        public event EventHandler<ConnectionCompleteParams> OnConnectionComplete;
        public event EventHandler<QueryCompleteParams> OnQueryComplete;
        public event EventHandler<IntelliSenseReadyParams> OnIntellisenseReady;
        public event EventHandler<MessageParams> OnQueryMessage;

        public async Task<bool> ConnectAsync(Uri ownerUri, string connectionStr)
        {
            var connectionOptions = new Dictionary<string, string>();
            connectionOptions.Add("ConnectionString", connectionStr);

            var connectionDetails = new ConnectionDetails() { Options = connectionOptions };
            var connectionParams = new ConnectParams() { OwnerUri = ownerUri.ToString(), Connection = connectionDetails };

            return await _rpc.InvokeWithParameterObjectAsync<bool>("connection/connect", connectionParams);
        }

        public async Task<bool> DisconnectAsync(Uri ownerUri)
        {
            var disconnectParams = new DisconnectParams() { OwnerUri = ownerUri.ToString() };
            return await _rpc.InvokeWithParameterObjectAsync<bool>("connection/disconnect", disconnectParams);
        }

        public async Task<IEnumerable<CompletionItem>> ProvideCompletionItemsAsync(Uri fileUri, RequestCompletions command)
        {
            string oldFileContents = await UpdateFileContentsAsync(fileUri, command.Code);

            TextDocumentIdentifier docId = new TextDocumentIdentifier() { Uri = fileUri.ToString() };
            Position position = new Position() { Line = command.LinePosition.Line, Character = command.LinePosition.Character };
            CompletionContext context = new CompletionContext() { TriggerKind = (int)CompletionTriggerKind.Invoked };
            var completionParams = new CompletionParams() { TextDocument = docId, Position = position, Context = context };

            var sqlCompletionItems = await _rpc.InvokeWithParameterObjectAsync<SqlCompletionItem[]>("textDocument/completion", completionParams);

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

        /// <summary>
        /// Updates the contents of the file at the provided path with the provided string,
        /// and then returns the old file contents as a string. If the file contents have
        /// changed, then a text change notification is also sent to the tools service.
        /// </summary>
        private async Task<string> UpdateFileContentsAsync(Uri fileUri, string newContents)
        {
            string oldFileContents = await File.ReadAllTextAsync(fileUri.LocalPath);
            if (!oldFileContents.Equals(newContents))
            {
                await File.WriteAllTextAsync(fileUri.LocalPath, newContents);

                await SendTextChangeNotificationAsync(fileUri, newContents, oldFileContents);
            }
            return oldFileContents;
        }

        public async Task<QueryExecuteResult> ExecuteQueryStringAsync(Uri ownerUri, string queryString)
        {
            var queryParams = new QueryExecuteStringParams() { Query = queryString, OwnerUri = ownerUri.ToString() };
            return await _rpc.InvokeWithParameterObjectAsync<QueryExecuteResult>("query/executeString", queryParams);
        }

        public async Task<QueryExecuteSubsetResult> ExecuteQueryExecuteSubsetAsync(QueryExecuteSubsetParams subsetParams)
        {
            return await _rpc.InvokeWithParameterObjectAsync<QueryExecuteSubsetResult>("query/subset", subsetParams);
        }

        public async Task SendTextChangeNotificationAsync(Uri ownerUri, string newText, string oldText)
        {
            var oldTextLines = oldText.Split('\n');
            var lastLineNum = Math.Max(0, oldTextLines.Length - 1);
            var lastLine = oldTextLines[lastLineNum];
            var lastCharacterNum = Math.Max(0, lastLine.Length - 1);

            var startPosition = new Position() { Line = 0, Character = 0 };
            var endPosition = new Position() { Line = lastLineNum, Character = lastCharacterNum };

            var textDoc = new VersionedTextDocumentIdentifier() { Uri = ownerUri.ToString(), Version = 1 };
            var changeRange = new Range() { Start = startPosition, End = endPosition };
            var docChange = new TextDocumentChangeEvent() { Text = newText, Range = changeRange };
            var changes = new TextDocumentChangeEvent[] { docChange };
            var textChangeParams = new DidChangeTextDocumentParams() { TextDocument = textDoc, ContentChanges = changes };

            await _rpc.NotifyWithParameterObjectAsync("textDocument/didChange", textChangeParams);
        }

        public void HandleConnectionCompletion(ConnectionCompleteParams connParams)
        {
            OnConnectionComplete(this, connParams);
        }

        public void HandleQueryCompletion(QueryCompleteParams queryParams)
        {
            OnQueryComplete(this, queryParams);
        }

        public void HandleQueryMessage(MessageParams messageParams)
        {
            OnQueryMessage(this, messageParams);
        }

        public void HandleIntellisenseReady(IntelliSenseReadyParams readyParams)
        {
            OnIntellisenseReady(this, readyParams);
        }

        public void Dispose()
        {
            _rpc.Dispose();
            _process.Kill(true);
            _process.Dispose();
        }
    }
}
