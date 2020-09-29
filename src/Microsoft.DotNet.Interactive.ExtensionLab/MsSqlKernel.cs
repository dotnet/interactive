// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class MsSqlKernel : SqlKernel
    {
        private readonly string _connectionString;

        public MsSqlKernel(string name, string connectionString) : base(name)
        {
            _connectionString = connectionString;
        }

        protected override DbConnection OpenConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}