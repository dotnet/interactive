// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit.Sdk;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public class JupyterConnectionTestData
{
    private readonly string _type;
    private readonly string _connectionString;
    private TestJupyterConnectionOptions _connectionOptions;

    public JupyterConnectionTestData(string type, TestJupyterConnectionOptions connectionOptions = null, string connectionString = "")
    {
        _type = type;
        _connectionString = connectionString;
        _connectionOptions = connectionOptions;
    }

    public string ConnectionType => _type;
    public string GetConnectionString() => _connectionString;
    public TestJupyterConnectionOptions GetConnectionOptions([CallerFilePath] string filePath = "", [CallerMemberName] string fileName = "")
    {
        if (_connectionOptions is null)
        {
            _connectionOptions = new TestJupyterConnectionOptions(KernelSpecName, filePath, fileName);
        }
        return _connectionOptions;
    }
    public string KernelSpecName { get; set; }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal class JupyterTestDataAttribute : DataAttribute
{
    private readonly object[] _data;
    public const string JUPYTER_TEST = nameof(JUPYTER_TEST);

    public JupyterTestDataAttribute(params object[] data)
    {
        _data = data;
    }

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        List<object> testData = new();
        var connData = GetConnectionTestData();
        connData.KernelSpecName = KernelSpecName;
        testData.Add(connData);
        testData.AddRange(_data);
        return new[] { testData.ToArray() };
    }
    protected virtual JupyterConnectionTestData GetConnectionTestData()
    {
        return new JupyterConnectionTestData(JUPYTER_TEST);
    }

    public string KernelSpecName { get; set; }
}

internal sealed class JupyterZMQTestDataAttribute : JupyterTestDataAttribute
{
    public const string TEST_DOTNET_JUPYTER_ZMQ_CONN = nameof(TEST_DOTNET_JUPYTER_ZMQ_CONN);
    public const string JUPYTER_ZMQ = nameof(JUPYTER_ZMQ);
    private static readonly string _skipReason;

    static JupyterZMQTestDataAttribute()
    {
        _skipReason = TestConnectionAndReturnSkipReason();
    }

    public JupyterZMQTestDataAttribute(params object[] data) : base(data)
    {
        if (_skipReason is not null)
        {
            Skip = _skipReason;
        }
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

    protected override JupyterConnectionTestData GetConnectionTestData()
    {
        return new JupyterConnectionTestData(JUPYTER_ZMQ,
            new TestJupyterConnectionOptions(new JupyterLocalKernelConnectionOptions(), KernelSpecName, AllowPlayback),
            GetConnectionString()
            );
    }

    /// <summary>
    /// Allows playing back the record of the connection by enabling save to a file 
    /// and reading back from it at the point of save.
    /// </summary>
    public bool AllowPlayback { get; set; }
}

internal sealed class JupyterHttpTestDataAttribute : JupyterTestDataAttribute
{
    public const string TEST_DOTNET_JUPYTER_HTTP_CONN = nameof(TEST_DOTNET_JUPYTER_HTTP_CONN);
    public const string JUPYTER_HTTP = nameof(JUPYTER_HTTP);
    private static readonly string _skipReason;

    static JupyterHttpTestDataAttribute()
    {
        _skipReason = TestConnectionAndReturnSkipReason();
    }

    public JupyterHttpTestDataAttribute(params object[] data) : base(data)
    {
        if (_skipReason is not null)
        {
            Skip = _skipReason;
        }
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

    protected override JupyterConnectionTestData GetConnectionTestData()
    {
        return new JupyterConnectionTestData(JUPYTER_HTTP,
                new TestJupyterConnectionOptions(new JupyterHttpKernelConnectionOptions(), KernelSpecName, AllowPlayback),
                GetConnectionString()
            );
    }

    /// <summary>
    /// Allows playing back the record of the connection by enabling save to a file 
    /// and reading back from it at the point of save.
    /// </summary>
    public bool AllowPlayback { get; set; }
}

