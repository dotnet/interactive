// // Copyright (c) .NET Foundation and contributors. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public enum CompletionTriggerKind {
        /**
        * Completion was triggered by typing an identifier (24x7 code
        * complete), manual invocation (e.g Ctrl+Space) or via API.
        */
        Invoked = 1,
        /**
        * Completion was triggered by a trigger character specified by
        * the `triggerCharacters` properties of the `CompletionRegistrationOptions`.
        */
        TriggerCharacter = 2,
        /**
        * Completion was re-triggered as the current completion list is incomplete.
        */
        TriggerForIncompleteCompletions = 3
    }

    public enum SqlCompletionItemKind
    {
        Text = 1,
        Method = 2,
        Function = 3,
        Constructor = 4,
        Field = 5,
        Variable = 6,
        Class = 7,
        Interface = 8,
        Module = 9,
        Property = 10,
        Unit = 11,
        Value = 12,
        Enum = 13,
        Keyword = 14,
        Snippet = 15,
        Color = 16,
        File = 17,
        Reference = 18
    }

    public class SqlCompletionItem
    {
        public string Label { get; set; }

        public SqlCompletionItemKind? Kind { get; set; }

        public string Detail { get; set; }

        /// <summary>
        /// Gets or sets the documentation string for the completion item.
        /// </summary>
        public string Documentation { get; set; }

        public string SortText { get; set; }

        public string FilterText { get; set; }

        public string InsertText { get; set; }

        public TextEdit TextEdit { get; set; }

        /// <summary>
        /// Gets or sets a custom data field that allows the server to mark
        /// each completion item with an identifier that will help correlate
        /// the item to the previous completion request during a completion
        /// resolve request.
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Exposing a command field for a completion item for passing telemetry
        /// </summary>
        public CompletionCommand Command { get; set; }

        /// <summary>
        /// Whether this completion item is preselected or not
        /// </summary>
        public bool? Preselect { get; set; }
    }

    public class CompletionCommand
    {
        /// <summary>
        /// Title of the command.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The identifier of the actual command handler, like `vsintellicode.completionItemSelected`.
        /// </summary>
        public string command { get; set; }

        /// <summary>
        /// A tooltip for the command, when represented in the UI.
        /// </summary>
        public string Tooltip { get; set; }

        /// <summary>
        /// Arguments that the command handler should be invoked with.
        /// </summary>
        public object[] Arguments { get; set; }
    }

    public class ConnectParams
    {
        public string OwnerUri { get; set; }
        public ConnectionDetails Connection { get; set; }
    }

    public class ConnectionDetails
    {
        public Dictionary<string, string> Options { get; set; }
    }

    public class DisconnectParams
    {
        public string OwnerUri { get; set; }
    }

    public class ExecuteStringParams
    {
        public string OwnerUri { get; set; }
        public string QueryString { get; set; }
    }

    public class CommandArgs
    {
        public string Title { get; set; }
        public string Command { get; set; }
        public string[] Arguments { get; set; }
    }

    public class TextEdit
    {
        public Range Range { get; set; }
        public string NewText { get; set; }
    }


    public class CompletionContext
    {
        public int TriggerKind { get; set; }
        public string TriggerCharacter { get; set; }
    }

    public class TextDocumentIdentifier
    {
        public string Uri { get; set; }
    }

    public class Position
    {
        public int Line { get; set; }
        public int Character { get; set; }
    }
    public class CompletionParams
    {
        public TextDocumentIdentifier TextDocument { get; set; }
        public Position Position { get; set; }
        public CompletionContext Context { get; set; }
    }

    public class TextDocumentPositionParams
    {
        public TextDocumentIdentifier TextDocument { get; set; }
        public Position Position { get; set; }
    }

    public class MarkedString
    {
        public string Language { get; set; }

        public string Value { get; set; }
    }

    public class Hover
    {
        public MarkedString[] Contents { get; set; }

        public Range? Range { get; set; }
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

    public class ExecuteRequestResult
    {
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

    /// <summary>
    /// Provides high level information about a connection.
    /// </summary>
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

    /// <summary>
    /// Parameters to be sent back with a query execution complete event
    /// </summary>
    public class QueryCompleteParams
    {
        /// <summary>
        /// URI for the editor that owns the query
        /// </summary>
        public string OwnerUri { get; set; }

        /// <summary>
        /// Summaries of the result sets that were returned with the query
        /// </summary>
        public BatchSummary[] BatchSummaries { get; set; }
    }

    /// <summary>
    /// Summary of a batch within a query
    /// </summary>
    public class BatchSummary
    {
        /// <summary>
        /// Localized timestamp for how long it took for the execution to complete
        /// </summary>
        public string ExecutionElapsed { get; set; }

        /// <summary>
        /// Localized timestamp for when the execution completed.
        /// </summary>
        public string ExecutionEnd { get; set; }

        /// <summary>
        /// Localized timestamp for when the execution started.
        /// </summary>
        public string ExecutionStart { get; set; }

        /// <summary>
        /// Whether or not the batch encountered an error that halted execution
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// The ID of the result set within the query results
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The selection from the file for this batch
        /// </summary>
        public SelectionData Selection { get; set; }

        /// <summary>
        /// The summaries of the result sets inside the batch
        /// </summary>
        public ResultSetSummary[] ResultSetSummaries { get; set; }
    }

    public class SelectionData
    {
        public int EndColumn { get; set; }

        public int EndLine { get; set; }

        public int StartColumn { get; set; }
        public int StartLine { get; set; }
    }

    /// <summary>
    /// Represents a summary of information about a result without returning any cells of the results
    /// </summary>
    public class ResultSetSummary
    {
        /// <summary>
        /// The ID of the result set within the batch results
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The ID of the batch set within the query
        /// </summary>
        public int BatchId { get; set; }

        /// <summary>
        /// The number of rows that are available for the resultset thus far
        /// </summary>
        public long RowCount { get; set; }

        /// <summary>
        /// If true it indicates that all rows have been fetched and the RowCount being sent across is final for this ResultSet
        /// </summary>
        public bool Complete { get; set; }

        /// <summary>
        /// Details about the columns that are provided as solutions
        /// </summary>
        public ColumnInfo[] ColumnInfo { get; set; }
    }

    public class SimpleExecuteParams
    {
		public string QueryString { get; set; }
		public string OwnerUri { get; set; }
	}

	public class SimpleExecuteResult
    {
		public int RowCount;
		public ColumnInfo[] ColumnInfo;
		public CellValue[][] Rows;
	}
}