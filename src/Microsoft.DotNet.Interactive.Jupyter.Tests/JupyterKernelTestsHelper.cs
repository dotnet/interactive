// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter.Connection;
using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit.Sdk;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public class JupyterConnectionTestData
{
    private readonly string _type;
    private readonly string _connectionString;
    private readonly TestJupyterConnectionOptions _connectionOptions;
    
    public JupyterConnectionTestData(string type, IJupyterKernelConnectionOptions connectionOptions = null, string connectionString = "")
    {
        _type = type;
        _connectionString = connectionString;
        _connectionOptions = new TestJupyterConnectionOptions(connectionOptions);
    }

    public string ConnectionType => _type;
    public string GetConnectionString() => _connectionString;
    public TestJupyterConnectionOptions GetConnectionOptions() => _connectionOptions;
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal sealed class JupyterZMQTestDataAttribute : DataAttribute
{
    public const string TEST_DOTNET_JUPYTER_ZMQ_CONN = nameof(TEST_DOTNET_JUPYTER_ZMQ_CONN);
    public const string JUPYTER_ZMQ = nameof(JUPYTER_ZMQ);
    private static readonly string _skipReason;
    private readonly object[] _data;
    
    
    static JupyterZMQTestDataAttribute()
    {
        _skipReason = TestConnectionAndReturnSkipReason();
    }

    public JupyterZMQTestDataAttribute(params object[] data)
    {
        _data = data;
        if (_skipReason is not null)
        {
            Skip = _skipReason;
        }
    }

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        List<object> testData = new();
        testData.Add(new JupyterConnectionTestData(JUPYTER_ZMQ, new JupyterLocalKernelConnectionOptions()));
        testData.AddRange(_data);
        return new[] { testData.ToArray() };
    }

    internal static string TestConnectionAndReturnSkipReason()
    {
        string connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return $"Environment variable {TEST_DOTNET_JUPYTER_ZMQ_CONN} is not set. To run tests that require "
                   + "Jupyter server running, this environment variable must be set";
        }

        return null;
    }

    public static string GetConnectionString()
    {
        return Environment.GetEnvironmentVariable(TEST_DOTNET_JUPYTER_ZMQ_CONN);
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal sealed class JupyterHttpTestDataAttribute : DataAttribute
{
    public const string TEST_DOTNET_JUPYTER_HTTP_CONN = nameof(TEST_DOTNET_JUPYTER_HTTP_CONN);
    public const string JUPYTER_HTTP = nameof(JUPYTER_HTTP);
    private static readonly string _skipReason;
    private readonly object[] _data;

    static JupyterHttpTestDataAttribute()
    {
        _skipReason = TestConnectionAndReturnSkipReason();
    }

    public JupyterHttpTestDataAttribute(params object[] data)
    {
        _data = data;
        if (_skipReason is not null)
        {
            Skip = _skipReason;
        }
    }
    
    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        List<object> testData = new();
        testData.Add(new JupyterConnectionTestData(JUPYTER_HTTP, new JupyterHttpKernelConnectionOptions(), GetConnectionString()));
        testData.AddRange(_data);
        return new[] { testData.ToArray() };
    }

    internal static string TestConnectionAndReturnSkipReason()
    {
        string connectionString = GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return $"Environment variable {TEST_DOTNET_JUPYTER_HTTP_CONN} is not set. To run tests that require "
                   + "Jupyter server running, this environment variable must be set to a valid connection string value with --url and --token.";
        }

        return null;
    }

    public static string GetConnectionString()
    {
        // e.g. --url <server> --token <token>
        return Environment.GetEnvironmentVariable(TEST_DOTNET_JUPYTER_HTTP_CONN);
    }
}

