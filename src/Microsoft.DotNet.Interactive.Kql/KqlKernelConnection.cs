// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Kusto.Data;
using Kusto.Data.Security;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.SqlServer;

namespace Microsoft.DotNet.Interactive.Kql
{
    public class KqlKernelConnection : ConnectKernelCommand<KqlConnectionOptions>
    {
        public KqlKernelConnection()
            : base("kql", "Connects to a Microsoft Kusto Server database")
        {
            Add(new Option<string>(
                "--cluster",
                "The cluster used to connect") {IsRequired = true});
            Add(new Option<string>(
                "--database",
                "The database to query"));
        }

        public override async Task<Kernel> CreateKernelAsync(
            KqlConnectionOptions options,
            KernelInvocationContext context)
        {
            var connectionDetails = await BuildConnectionDetailsAsync(options);
            
            var root = Kernel.Root.FindResolvedPackageReference();

            var pathToService = root.PathToService("MicrosoftKustoServiceLayer");

            var sqlClient = new ToolsServiceClient(pathToService);

            var kernel = new MsKqlKernel(
                $"kql-{options.KernelName}",
                connectionDetails, 
                sqlClient);

            await kernel.ConnectAsync();

            return kernel;
        }

        private async Task<KqlConnectionDetails> BuildConnectionDetailsAsync(KqlConnectionOptions options)
        {
            return new KqlConnectionDetails
            {
                Cluster = options.Cluster,
                Database = options.Database,
                Token = await GetKustoTokenAsync(options)
            };
        }

        private static async Task<string> GetKustoTokenAsync(KqlConnectionOptions options)
        {
            var kcsb = new KustoConnectionStringBuilder(options.Cluster, options.Database)
                .WithAadUserPromptAuthentication();
            var authenticator = HttpClientAuthenticatorFactory.CreateAuthenticator(kcsb);
            
            var request = new HttpRequestMessage();
            await authenticator.AuthenticateAsync(request);

            // first value of authorization is the auth token
            // stored in <bearer> <token> format
            return request.Headers.GetValues("Authorization").First().Split(' ').Last();
        }
    }
}
