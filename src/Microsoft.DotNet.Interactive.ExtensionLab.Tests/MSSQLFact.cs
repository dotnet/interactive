// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;

using Xunit;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests
{
    public sealed class MSSQLFact : FactAttribute
    {
        public MSSQLFact(string connectionString)
        {
            try
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                connection.Dispose();
            }
            catch
            {
                Skip = "Required connection cannot be found or established";
            }
        }
    }
}