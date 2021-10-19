// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class SQLiteKernelConnector : IKernelConnector
    {
        public string ConnectionString { get; }
        public Task<Kernel> ConnectKernelAsync(KernelInfo kernelInfo)
        {
            var kernel = new SQLiteKernel(
                $"sql-{kernelInfo}",
                ConnectionString);

            return Task.FromResult<Kernel>(kernel);
        }

        public SQLiteKernelConnector(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }
}