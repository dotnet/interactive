// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.SqlServer
{
    public class MsSqlKernelConnection : KernelConnection
    {
        public bool CreateDbContext { get; set; }

        public string ConnectionString { get; set; }

        public string PathToService { get; set; }

        public override async Task<Kernel> ConnectKernelAsync()
        {
            var sqlClient = new ToolsServiceClient(PathToService, $"--parent-pid {Process.GetCurrentProcess().Id}");

            var kernel = new MsSqlKernel(
                $"sql-{KernelName}",
                ConnectionString,
                sqlClient);

            kernel.RegisterForDisposal(sqlClient);

            await kernel.ConnectAsync();

            return kernel;
        }
    }
}