// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Kusto.Data;
using Kusto.Data.Security;
using Microsoft.DotNet.Interactive.SqlServer;

namespace Microsoft.DotNet.Interactive.Kql;

internal class KqlKernelConnector
{
    public KqlKernelConnector(string cluster, string database)
    {
        Cluster = cluster;
        Database = database;
    }

    public string Cluster { get; }

    public string Database { get; }

    internal string PathToService { get; set; }

    public async Task<Kernel> CreateKernelAsync(string kernelName)
    {
        if (string.IsNullOrWhiteSpace(PathToService))
        {
            throw new InvalidOperationException($"{nameof(PathToService)} cannot be null or whitespace.");
        }

        var connectionDetails = await BuildConnectionDetailsAsync();

        var serviceArgs = $"--parent-pid {Environment.ProcessId}";
        var logFile = Environment.GetEnvironmentVariable("DOTNET_KUSTOSERVICE_LOGFILE");
        if (!string.IsNullOrWhiteSpace(logFile))
        {
            var logArgs = $" --log-file \"{logFile}\" --tracing-level Verbose";
            serviceArgs += logArgs;
        }

        var client = new ToolsServiceClient(PathToService, serviceArgs);

        var kernel = new MsKqlKernel(
                $"kql-{kernelName}",
                connectionDetails,
                client)
            .UseValueSharing();

        kernel.RegisterForDisposal(client);

        await kernel.ConnectAsync();

        return kernel;
    }

    private async Task<KqlConnectionDetails> BuildConnectionDetailsAsync()
    {
        return new KqlConnectionDetails
        {
            Cluster = Cluster,
            Database = Database,
            Token = await GetKustoTokenAsync()
        };
    }

    private async Task<string> GetKustoTokenAsync()
    {
        var kcsb = new KustoConnectionStringBuilder(Cluster, Database)
            .WithAadUserPromptAuthentication();
        var authenticator = HttpClientAuthenticatorFactory.CreateAuthenticator(kcsb);

        var request = new HttpRequestMessage();
        await authenticator.AuthenticateAsync(request);

        // first value of authorization is the auth token
        // stored in <bearer> <token> format
        return request.Headers.GetValues("Authorization").First().Split(' ').Last();
    }
}