﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.ExtensionLab;

public class SQLiteKernelConnector : IKernelConnector
{
    public SQLiteKernelConnector(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public Task<Kernel> CreateKernelAsync(string kernelName)
    {
        var kernel = new SQLiteKernel(
            $"sql-{kernelName}",
            ConnectionString);

        return Task.FromResult<Kernel>(kernel);
    }
}