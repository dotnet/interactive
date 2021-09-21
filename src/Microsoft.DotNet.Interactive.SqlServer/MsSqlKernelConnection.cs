// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.SqlServer
{
    public class MsSqlKernelConnection : KernelConnection
    {
        public bool CreateDbContext { get;  }

        public string ConnectionString { get;  }

        public string PathToService { get; set; }

        public override async Task<Kernel> ConnectKernelAsync()
        {
            if (string.IsNullOrWhiteSpace(PathToService))
            {
                throw new InvalidOperationException($"{nameof(PathToService)} cannot be null or whitespace.");
            }

            var sqlClient = new ToolsServiceClient(PathToService, $"--parent-pid {Process.GetCurrentProcess().Id}");

            var kernel = new MsSqlKernel(
                $"sql-{KernelName}",
                ConnectionString,
                sqlClient);

            kernel.RegisterForDisposal(sqlClient);

            await kernel.ConnectAsync();

            return kernel;
        }

        public MsSqlKernelConnection(string kernelName, bool createDbContext, string connectionString) : base(kernelName)
        {
            CreateDbContext = createDbContext;
            ConnectionString = connectionString;
        }
    }
}