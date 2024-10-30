// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.DuckDB;

public class DuckDBKernelConnector(string connectionString)
{
    public string ConnectionString { get; } = connectionString;

    public Task<Kernel> CreateKernelAsync(string kernelName)
    {
        var kernel = new DuckDBKernel(
            $"{kernelName}",
            ConnectionString);

        return Task.FromResult<Kernel>(kernel);
    }
}