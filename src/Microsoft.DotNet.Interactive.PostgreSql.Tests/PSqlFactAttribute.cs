// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Data.SqlClient;
using Xunit;

namespace Microsoft.DotNet.Interactive.PostgreSql.Tests;

public sealed class PSqlFactAttribute : FactAttribute
{
    private const string TEST_PSQL_CONNECTION_STRING = nameof(TEST_PSQL_CONNECTION_STRING);
    private static readonly string _skipReason;

    static PSqlFactAttribute()
    {
        _skipReason = TestConnectionAndReturnSkipReason();
    }

    public PSqlFactAttribute()
    {
        if (_skipReason is not null)
        {
            Skip = _skipReason;
        }
    }

    internal static string TestConnectionAndReturnSkipReason()
    {
        string connectionString = GetConnectionStringForTests();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return $"Environment variable {TEST_PSQL_CONNECTION_STRING} is not set. To run tests that require "
                   + "SQL Server, this environment variable must be set to a valid connection string value.";
        }

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
        }
        catch (Exception e)
        {
            return $"A connection could not be established to SQL Server. Verify the connection string value used " +
                   $"for environment variable {TEST_PSQL_CONNECTION_STRING} targets a running SQL Server instance. " +
                   $"Connection failed failed with error: {e}";
        }

        return null;
    }

    public static string GetConnectionStringForTests()
    {
        // example:
        // Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=postgres
        return Environment.GetEnvironmentVariable(TEST_PSQL_CONNECTION_STRING);
    }
}