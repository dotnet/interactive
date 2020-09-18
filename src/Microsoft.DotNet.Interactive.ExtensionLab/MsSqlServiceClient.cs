// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Utility;
using Newtonsoft.Json;
using StreamJsonRpc;
using System.Data.Common;
using System.Data;

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

            var connDelegate = new HandleConnectionDelegate(HandleConnectionCompletion);
            rpc.AddLocalRpcMethod("connection/complete", connDelegate);

            rpc.StartListening();
        }

        private delegate void HandleConnectionDelegate(ConnectionCompleteParams connParams);

        public event EventHandler<ConnectionCompleteParams> OnConnectionComplete;

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

        public async Task<CompletionItem[]> ProvideCompletionItemsAsync()
        {
            //TextDocumentIdentifier docId = new TextDocumentIdentifier() { uri = "/Users/vasubhog/Desktop/test.sql" };
            //Position position = new Position() { line = 1, character = 2 };
            //CompletionContext context = new CompletionContext() { triggerKind = 1, triggerCharacter = null };
            //var completionParams = new CompletionParams() { textDocument = docId, position = position, workDoneToken = null, context = context, partialResultToken = null };
            //var result = await rpc.InvokeWithParameterObjectAsync<CompletionItem[]>("textDocument/completion", completionParams);
            //return result;
            await Task.CompletedTask;
            return new CompletionItem[0];
        }

        public async Task<SimpleExecuteResult> ExecuteQueryStringAsync(string ownerUri, string queryString)
        {
            var queryParams = new SimpleExecuteParams() { OwnerUri = ownerUri, QueryString = queryString };
            return await rpc.InvokeWithParameterObjectAsync<SimpleExecuteResult>("query/simpleexecute", queryParams);
        }

        public async Task<QueryExecuteSubsetResult> ExecuteQueryExecuteSubsetAsync(string ownerUri)
        {
            var queryExecuteSubsetParams = new QueryExecuteSubsetParams() { OwnerUri = ownerUri, ResultSetIndex = 0, RowsCount = 1 };
            return await rpc.InvokeWithParameterObjectAsync<QueryExecuteSubsetResult>("query/subset", queryExecuteSubsetParams);
        }

        public void HandleConnectionCompletion(ConnectionCompleteParams connectionSummary)
        {
            return;
        }

        public void Dispose()
        {
            rpc.Dispose();
            process.Dispose();
        }
    }

#region Protocol Objects
    public class ConnectParams
    {
        public string OwnerUri;
        public ConnectionDetails Connection;
    }

    public class ConnectionDetails
    {
        public Dictionary<string, string> Options;

        // public string Password = "";
        public string ServerName = "localhost";
        public string DatabaseName = "tempdb";
        // public string UserName = "";
        public string AuthenticationType = "Integrated";
        // public string ApplicationName = "";
        // public string WorkstationId = "";
        // public string ApplicationIntent = "";
        // public string CurrentLanguage = "";
        // public string AttachDbFilename = "";
        // public string FailoverPartner = "";
        // public string TypeSystemVersion = "";
        // public string ConnectionString = "";
        // public string GroupId = "";
        // public string DatabaseDisplayName = "";
        // public string AzureAccountToken = "";
        // public bool IsComparableTo(ConnectionDetails other)
        // {
        //     return true;
        // }

    }

    public class DisconnectParams
    {
        public string OwnerUri;
    }

    public class SimpleExecuteParams
    {
        public string OwnerUri;
        public string QueryString;
    }

    public class CompletionItem
    {
        public string label;
        public int kind;
        public int[] tags; //CompletionItemTag
        public string detail;
        public string documentation; // | MarkupContent
        public bool deprecated;
        public bool preselect;
        public string sortText;
        public string filterText;
        public string insertText;
        int insertTextFormat; //InsertTextFormat
        TextEdit textEdit;
        TextEdit[] additionalTextEdits;
        string[] commitCharacters;
        CommandArgs command;
        public string data;
    }

    public class CommandArgs
    {
        public string title;
        public string Command;
        public string[] arguments;
    }

    public class TextEdit
    {
        public Range range;
        public string newText;
    }


    public class CompletionContext
    {
        public int triggerKind; // CompletionTriggerKind
        public string triggerCharacter;
    }

    public class TextDocumentIdentifier
    {
        public string uri; //DocumentUri
    }

    public class Position
    {
        public int line;
        public int character;
    }
    public class CompletionParams
    {
        public TextDocumentIdentifier textDocument;
        public Position position;
        public string workDoneToken; //ProgressToken = number | string
        public string partialResultToken; //ProgressToken = number | string
        public CompletionContext context;
    }

    public class QueryExecuteSubsetParams
    {
        /// <summary>
        /// URI for the file that owns the query to look up the results for
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Index of the batch to get the results from
        /// </summary>
        public int BatchIndex { get; set; }

        /// <summary>
        /// Index of the result set to get the results from
        /// </summary>
        public int ResultSetIndex;

        /// <summary>
        /// Beginning index of the rows to return from the selected resultset. This index will be
        /// included in the results.
        /// </summary>
        public int RowsStartIndex { get; set; }

        /// <summary>
        /// Number of rows to include in the result of this request. If the number of the rows 
        /// exceeds the number of rows available after the start index, all available rows after
        /// the start index will be returned.
        /// </summary>
        public int RowsCount { get; set; }
    }

    public class SimpleExecuteResult
    {
        public int RowCount;
        public ColumnInfo[] ColumnInfo;
        public CellValue[][] Rows;
    }

    public class QueryExecuteSubsetResult
    {
        /// <summary>
        /// Subset request error messages. Optional, can be set to null to indicate no errors
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The requested subset of results. Optional, can be set to null to indicate an error
        /// </summary>
        public ResultSetSubset ResultSubset { get; set; }
    }

    public class ResultSetSubset
    {
        /// <summary>
        /// The number of rows returned from result set, useful for determining if less rows were
        /// returned than requested.
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// 2D array of the cell values requested from result set
        /// </summary>
        public CellValue[][] Rows { get; set; }
    }

    public class ColumnInfo
    {
		public bool? AllowDBNull { get; set; }
		public string BaseCatalogName { get; set; }
		public string BaseColumnName { get; set; }
		public string BaseSchemaName { get; set; }
		public string BaseServerName { get; set; }
		public string BaseTableName { get; set; }
		public string ColumnName { get; set; }
		public int? ColumnOrdinal { get; set; }
		public int? ColumnSize { get; set; }
		public bool? IsAliased { get; set; }
		public bool? IsAutoIncrement { get; set; }
		public bool? IsExpression { get; set; }
		public bool? IsHidden { get; set; }
		public bool? IsIdentity { get; set; }
		public bool? IsKey { get; set; }
		public bool? IsBytes { get; set; }
		public bool? IsChars { get; set; }
		public bool? IsSqlVariant { get; set; }
		public bool? IsUdt { get; set; }
		public string DataType { get; set; }
		public bool? IsXml { get; set; }
		public bool? IsJson { get; set; }
		public bool? IsLong { get; set; }
		public bool? IsReadOnly { get; set; }
		public bool? IsUnique { get; set; }
		public int? NumericPrecision { get; set; }
		public int? NumericScale { get; set; }
		public string UdtAssemblyQualifiedName { get; set; }
		public string DataTypeName { get; set; }
	}

    public class CellValue
    {
        /// <summary>
        /// Display value for the cell, suitable to be passed back to the client
        /// </summary>
        public string DisplayValue { get; set; }

        /// <summary>
        /// Whether or not the cell is NULL
        /// </summary>
        public bool IsNull { get; set; }

        /// <summary>
        /// Culture invariant display value for the cell, this value can later be used by the client to convert back to the original value.
        /// </summary>
        public string InvariantCultureDisplayValue { get; set; }

        /// <summary>
        /// The raw object for the cell, for use internally
        /// </summary>
        internal object RawObject { get; set; }

        /// <summary>
        /// The internal ID for the row. Should be used when directly referencing the row for edit
        /// or other purposes.
        /// </summary>
        public long RowId { get; set; }
    }

    public class ServerInfo
    {
        /// <summary>
        /// The major version of the SQL Server instance.
        /// </summary>
        public int ServerMajorVersion { get; set; }

        /// <summary>
        /// The minor version of the SQL Server instance.
        /// </summary>
        public int ServerMinorVersion { get; set; }

        /// <summary>
        /// The build of the SQL Server instance.
        /// </summary>
        public int ServerReleaseVersion { get; set; }

        /// <summary>
        /// The ID of the engine edition of the SQL Server instance.
        /// </summary>
        public int EngineEditionId { get; set; }

        /// <summary>
        /// String containing the full server version text.
        /// </summary>
        public string ServerVersion { get; set; }

        /// <summary>
        /// String describing the product level of the server.
        /// </summary>
        public string ServerLevel { get; set; }

        /// <summary>
        /// The edition of the SQL Server instance.
        /// </summary>
        public string ServerEdition { get; set; }

        /// <summary>
        /// Whether the SQL Server instance is running in the cloud (Azure) or not.
        /// </summary>
        public bool IsCloud { get; set; }

        /// <summary>
        /// The version of Azure that the SQL Server instance is running on, if applicable.
        /// </summary>
        public int AzureVersion { get; set; }

        /// <summary>
        /// The Operating System version string of the machine running the SQL Server instance.
        /// </summary>
        public string OsVersion { get; set; }

        /// <summary>
        /// The Operating System version string of the machine running the SQL Server instance.
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// Server options
        /// </summary>
        public Dictionary<string, object> Options { get; set; }
    }

    public class ConnectionCompleteParams
    {
        /// <summary>
        /// A URI identifying the owner of the connection. This will most commonly be a file in the workspace
        /// or a virtual file representing an object in a database.         
        /// </summary>
        public string OwnerUri { get; set;  }

        /// <summary>
        /// A GUID representing a unique connection ID
        /// </summary>
        public string ConnectionId { get; set; }

        /// <summary>
        /// Gets or sets any detailed connection error messages.
        /// </summary>
        public string Messages { get; set; }

        /// <summary>
        /// Error message returned from the engine for a connection failure reason, if any.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Error number returned from the engine for connection failure reason, if any.
        /// </summary>
        public int ErrorNumber { get; set; }

        /// <summary>
        /// Information about the connected server.
        /// </summary>
        public ServerInfo ServerInfo { get; set; }

        /// <summary>
        /// Gets or sets the actual Connection established, including Database Name
        /// </summary>
        public ConnectionSummary ConnectionSummary { get; set; }

        /// <summary>
        /// The type of connection that this notification is for
        /// </summary>
        public string Type { get; set; }
    }

    public class ConnectionSummary
    {
        /// <summary>
        /// Gets or sets the connection server name
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Gets or sets the connection database name
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the connection user name
        /// </summary>
        public string UserName { get; set; }
    }
#endregion
}
