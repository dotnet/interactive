// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Xunit;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests
{
    public sealed class MsSqlFact : FactAttribute
    {
        private const string TEST_MSSQL_CONNECTION_STRING = "TEST_MSSQL_CONNECTION_STRING";
        private static readonly string _skipReason;
        
        static MsSqlFact()
        {
            _skipReason = TestConnectionAndReturnSkipReason();
        }

        public MsSqlFact()
        {
            if (_skipReason != null)
            {
                this.Skip = _skipReason;
            }
        }
        
        private static string TestConnectionAndReturnSkipReason()
        {
            string connectionString = GetConnectionStringForTests();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return $"Environment variable {TEST_MSSQL_CONNECTION_STRING} is not set. To run tests that require "
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
                       $"for environment variable {TEST_MSSQL_CONNECTION_STRING} targets a running SQL Server instance. " +
                       $"Connection failed failed with error: {e}";                    
            }

            return null;
        }
        
        public static string GetConnectionStringForTests()
        {
            return Environment.GetEnvironmentVariable(TEST_MSSQL_CONNECTION_STRING);
        }  
    }
}