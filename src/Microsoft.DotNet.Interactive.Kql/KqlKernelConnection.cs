// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Kusto.Data;
using Kusto.Data.Security;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.SqlServer;

namespace Microsoft.DotNet.Interactive.Kql
{
    public class KqlKernelConnection : ConnectKernelCommand<KqlConnection>
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

        public override async Task<Kernel> ConnectKernelAsync(
            KqlConnection connection,
            KernelInvocationContext context)
        {
            var root = Kernel.Root.FindResolvedPackageReference();

            var pathToService = root.PathToService("MicrosoftKustoServiceLayer");

            connection.PathToService = pathToService;

            var kernel = await connection.ConnectKernelAsync();

            return kernel;
        }


    }
}
