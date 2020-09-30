// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient;

using Xunit;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests
{
    public sealed class MsSqlFact : FactAttribute
    {
        public MsSqlFact(string requiredConnectionString)
        {
            try
            {
                var connection = new SqlConnection(requiredConnectionString);
                connection.Open();
                connection.Dispose();
            }
            catch
            {
                Skip = "Required db connection cannot be found or established";
            }
        }
    }
}