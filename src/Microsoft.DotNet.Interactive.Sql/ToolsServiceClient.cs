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

namespace Microsoft.DotNet.Interactive.Sql
{

    public class ToolsServiceClient: IDisposable
    {
        private Process process;
        private JsonRpc rpc;

        public void startProcessAndRedirectIO()
        {
            var startInfo = new ProcessStartInfo("C:\\Microsoft.SqlTools.ServiceLayer-win-x64-netcoreapp3.1\\MicrosoftSqlToolsServiceLayer.exe")
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

            rpc = JsonRpc.Attach(process.StandardInput.BaseStream, process.StandardOutput.BaseStream);
        }

        public async Task<bool> ConnectAsync(string ownerUri, string connectionStr)
        {
            var connStrBuilder = new SqlConnectionStringBuilder(connectionStr);
            var connectionOptions = new Dictionary<string, string>();
            foreach(string optionKey in connStrBuilder.Keys)
            {
                object optionValue;
                if (connStrBuilder.TryGetValue(optionKey, out optionValue))
                {
                    connectionOptions.Add(optionKey, optionValue.ToString());
                }
            }
            var connectionInfo = new ConnectionInfo() { Options = connectionOptions };
            var connectionParams = new ConnectParams() { OwnerUri = ownerUri, Connection = connectionInfo };

            var result = await rpc.InvokeAsync<bool>("connection/connect", connectionParams);
            return result;
        }

        public async Task<bool> DisconnectAsync(string ownerUri)
        {
            var disconnectParams = new DisconnectParams() { OwnerUri = ownerUri };
            var result = await rpc.InvokeAsync<bool>("connection/disconnect", disconnectParams);
            return result;
        }

        public async Task<object> ExecuteQueryStringAsync(string ownerUri, string queryString)
        {
            var queryParams = new QueryExecuteStringParams() { OwnerUri = ownerUri, Query = queryString };
            var result = await rpc.InvokeAsync<object>("query/executeString", queryParams);
            return result;
        }

        public void Dispose()
        {
            rpc.Dispose();
            process.Dispose();
        }
    }

    public class ConnectParams
    {
        public string OwnerUri;
        public ConnectionInfo Connection;
    }

    public class ConnectionInfo
    {
	    public Dictionary<string, string> Options;
    }

    public class DisconnectParams {
        public string OwnerUri;
    }

    public class QueryExecuteStringParams
    {
        public string OwnerUri;
        public string Query;
    }

    public class ExecuteQueryRequest
    {
        public ExecuteQueryRequest()
        {
        }
    }
}
