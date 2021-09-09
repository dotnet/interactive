using System;
using Xunit;

namespace Microsoft.DotNet.Interactive.SqlServer.Tests
{
    public sealed class KqlFact : FactAttribute
    {
        private const string TEST_KQL_CONNECTION_STRING = nameof(TEST_KQL_CONNECTION_STRING);
        private static readonly string _skipReason;
        
        static KqlFact()
        {
            _skipReason = TestConnectionAndReturnSkipReason();
        }
        
        public KqlFact()
        {
            if (_skipReason is not null)
            {
                Skip = _skipReason;
            }
        }
        
        private static string TestConnectionAndReturnSkipReason()
        {
            string clusterName = GetClusterForTests();
            if (string.IsNullOrWhiteSpace(clusterName))
            {
                return $"Environment variable {TEST_KQL_CONNECTION_STRING} is not set. To run tests that require "
                       + "KQL Cluster, this environment variable must be set to a valid connection string value.";
            }

            return null;
        }
        
        public static string GetClusterForTests()
        {
            return "https://help.kusto.windows.net/"; //Environment.GetEnvironmentVariable(TEST_KQL_CONNECTION_STRING);
        }  
    }
}