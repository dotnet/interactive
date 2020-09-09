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

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class MsSqlKernel :
        Kernel,
        IKernelCommandHandler<SubmitCode>
    {
        private readonly string _connectionString;
        private readonly MsSqlServiceClient serviceClient;

        public MsSqlKernel(string name, string connectionString) : base(name)
        {
            _connectionString = connectionString;
            serviceClient = new MsSqlServiceClient();
            serviceClient.StartProcessAndRedirectIO();
        }

        public async Task<bool> ConnectAsync(string ownerUri)
        {
            return await serviceClient.ConnectAsync(ownerUri, _connectionString);
        }

        public async Task<bool> DisconnectAsync(string ownerUri)
        {
            return await serviceClient.DisconnectAsync(ownerUri);
        }

        public async Task<CompletionItem[]> ProvideCompletionItemsAsync()
        {
            return await serviceClient.ProvideCompletionItemsAsync();
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