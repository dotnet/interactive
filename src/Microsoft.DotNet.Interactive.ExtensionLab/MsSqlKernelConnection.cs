// // Copyright (c) .NET Foundation and contributors. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class MsSqlKernelConnection : ConnectKernelCommand<SqlConnectionOptions>
    {
        public MsSqlKernelConnection()
            : base("mssql", "Connects to a Microsoft SQL Server database - Dev version")
        {
            Add(new Argument<string>("connectionString", "The connection string used to connect to the database"));
        }

        public override Task<Kernel> CreateKernelAsync(
            SqlConnectionOptions options,
            KernelInvocationContext context)
        {
            var kernel = new MsSqlKernel(
                options.KernelName,
                options.ConnectionString);

            return Task.FromResult<Kernel>(kernel);
        }
    }
}