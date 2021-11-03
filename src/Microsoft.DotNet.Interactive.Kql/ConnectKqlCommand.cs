// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.SqlServer;

namespace Microsoft.DotNet.Interactive.Kql
{
    public class ConnectKqlCommand : ConnectKernelCommand<KqlKernelConnector>
    {
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
            var root = Kernel.Root.FindResolvedPackageReference();

            var pathToService = root.PathToService("MicrosoftKustoServiceLayer");

            connector.PathToService = pathToService;

            var kernel = await connector.ConnectKernelAsync(kernelInfo);

            return kernel;
        }


    }
}
