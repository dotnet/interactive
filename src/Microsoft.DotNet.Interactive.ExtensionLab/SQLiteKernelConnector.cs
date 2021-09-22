// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Documents;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class SQLiteKernelConnector : KernelConnector
    {
        public string ConnectionString { get; }
        public override Task<Kernel> ConnectKernelAsync(KernelName kernelName)
        {
            var kernel = new SQLiteKernel(
                $"sql-{kernelName}",
                ConnectionString);

            return Task.FromResult<Kernel>(kernel);
        }

        public SQLiteKernelConnector(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }
}