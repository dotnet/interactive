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

namespace Microsoft.DotNet.Interactive.SqlServer
{
    public class KustoKernelConnection : ConnectKernelCommand<KustoConnectionOptions>
    {
        public KustoKernelConnection()
            : base("kql", "Connects to a Microsoft Kusto Server database")
        {
            Add(new Option<string>(
                "--cluster",
                "The cluster used to connect") {IsRequired = true});
            Add(new Option<string>(
                "--database",
                "The database to query"));
            Add(new Option<bool>(
                "--login", 
                "Uses username and password authentication"));
        }

        public override async Task<Kernel> CreateKernelAsync(
            KustoConnectionOptions options,
            KernelInvocationContext context)
        {
            var connectionDetails = await BuildConnectionDetailsAsync(options);

            var resolvedPackageReferences = ((ISupportNuget)context.HandlingKernel).ResolvedPackageReferences;
            // Walk through the packages looking for the package that endswith the name "Microsoft.SqlToolsService"
            // and grab the packageroot
            var runtimePackageIdSuffix = "native.Microsoft.SqlToolsService";
            var root = resolvedPackageReferences.FirstOrDefault(p => p.PackageName.EndsWith(runtimePackageIdSuffix, StringComparison.OrdinalIgnoreCase));
            string pathToService = "";
            
            if (root is not null)
            {
                // Extract the platform 'osx-x64' from the package name 'runtime.osx-x64.native.microsoft.sqltoolsservice'
                string[] packageNameSegments = root.PackageName.Split(".");
                if (packageNameSegments.Length > 2)
                {
                    string platform = packageNameSegments[1];
                    
                    // Build the path to the MicrosoftSqlToolsServiceLayer executable by reaching into the resolve nuget package
                    // assuming a convention for native binaries.
                    pathToService = Path.Combine(
                        root.PackageRoot,
                        "runtimes",
                        platform,
                        "native",
                        "MicrosoftKustoServiceLayer");
                    
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        pathToService += ".exe";
                    }
                }
            }

            var sqlClient = new MsSqlServiceClient(pathToService);

            var kernel = new MsKustoKernel(
                $"kusto-{options.KernelName}",
                connectionDetails, 
                sqlClient);

            await kernel.ConnectAsync();

            return kernel;
        }

        private async Task<KustoConnectionDetails> BuildConnectionDetailsAsync(KustoConnectionOptions options)
        {
            return new KustoConnectionDetails
            {
                Cluster = options.Cluster,
                Database = options.Database,
                Token = await GetKustoTokenAsync(options)
            };
        }

        private static async Task<string> GetKustoTokenAsync(KustoConnectionOptions options)
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
