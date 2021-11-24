// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.Kql
{
    public class ConnectKqlCommand : ConnectKernelCommand<KqlKernelConnector>
    {
        public static string ResolvedToolsServicePath { get; internal set; }

        public ConnectKqlCommand()
            : base("kql", "Connects to a Microsoft Kusto Server database")
        {
            Add(new Option<string>(
                "--cluster",
                "The cluster used to connect") {IsRequired = true});
            Add(new Option<string>(
                "--database",
                "The database to query"));
        }

        public override async Task<Kernel> ConnectKernelAsync(KernelInfo kernelInfo, KqlKernelConnector connector,
            KernelInvocationContext context)
        {
            connector.PathToService = ResolvedToolsServicePath;

            var kernel = await connector.ConnectKernelAsync(kernelInfo);

            return kernel;
        }


    }
}
