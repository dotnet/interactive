// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class SQLiteConnection : KernelConnection
    {
        public string ConnectionString { get; set; }
        public override Task<Kernel> ConnectKernelAsync()
        {
            var kernel = new SQLiteKernel(
                $"sql-{KernelName}",
                ConnectionString);

            return Task.FromResult<Kernel>(kernel);
        }
    }
}