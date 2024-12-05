using System;
using Xunit;

namespace Microsoft.DotNet.Interactive.Kql.Tests;

public sealed class KqlFactAttribute : FactAttribute
{
    private const string TEST_KQL_CONNECTION_STRING = nameof(TEST_KQL_CONNECTION_STRING);
    private static readonly string _skipReason;
        
    static KqlFactAttribute()
    {
        _skipReason = TestConnectionAndReturnSkipReason();
    }
        
    public KqlFactAttribute()
    {
        if (_skipReason is not null)
        {
            Skip = _skipReason;
        }
    }
        
    internal static string TestConnectionAndReturnSkipReason()
    {
        string clusterName = GetClusterForTests();
        if (string.IsNullOrWhiteSpace(clusterName))
        {
            return
                $"""
                 Environment variable {TEST_KQL_CONNECTION_STRING} is not set. To run tests that require a KQL Cluster, this environment variable must be set to a valid connection string value.
                 """;
        }

        return null;
    }
        
    public static string GetClusterForTests()
    {
        // e.g. https://help.kusto.windows.net
        return Environment.GetEnvironmentVariable(TEST_KQL_CONNECTION_STRING);
    }  
}