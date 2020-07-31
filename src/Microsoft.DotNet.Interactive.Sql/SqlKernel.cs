// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.LanguageService;

namespace Microsoft.DotNet.Interactive.Sql
{
    public class SqlKernel :
        KernelBase,
        IKernelCommandHandler<SubmitCode>
    {
        internal const string DefaultKernelName = "sql";
        private readonly ToolsServiceClient serviceClient;

        public SqlKernel(): base(DefaultKernelName)
        {
            serviceClient = new ToolsServiceClient();
            serviceClient.startProcessAndRedirectIO();
        }

        public async Task<bool> ConnectAsync(string ownerUri, string connectionStr)
        {
            return await serviceClient.ConnectAsync(ownerUri, connectionStr);
        }

        public async Task<bool> DisconnectAsync(string ownerUri)
        {
            return await serviceClient.DisconnectAsync(ownerUri);
        }

        public async Task<ExecuteRequestResult> ExecuteQueryStringAsync(string ownerUri, string queryString)
        {
            return await serviceClient.ExecuteQueryStringAsync(ownerUri, queryString);
        }

        public async Task<QueryExecuteSubsetResult> ExecuteQueryExecuteSubsetAsync(string ownerUri)
        {
            return await serviceClient.ExecuteQueryExecuteSubsetAsync(ownerUri);
        }

        public async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            // SEND Sql query
            // await completion
            // Get results
            // Display
            await context.DisplayAsync("HELLO WORLD");
        }
    }
}