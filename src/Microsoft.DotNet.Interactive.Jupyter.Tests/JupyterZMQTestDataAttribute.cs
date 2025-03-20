// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

internal sealed class JupyterZMQTestDataAttribute : JupyterTestDataAttribute, ITestDataSourceIgnoreCapability
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
            IgnoreMessage = _skipReason;
        }
    }

    internal static string TestConnectionAndReturnSkipReason()
    {
        var connectionString = GetConnectionString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return $"Environment variable {TEST_DOTNET_JUPYTER_ZMQ_CONN} is not set. To run tests that require Jupyter server running, this environment variable must be set.";
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
                                             new SimulatedJupyterConnectionOptions(new JupyterLocalKernelConnectionOptions(), KernelSpecName, AllowPlayback),
                                             GetConnectionString()
        );
    }

    /// <summary>
    /// Allows playing back the record of the connection by enabling save to a file 
    /// and reading back from it at the point of save.
    /// </summary>
    public bool AllowPlayback { get; set; }

    public string IgnoreMessage { get; set; }
}