// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

internal sealed class JupyterHttpTestDataAttribute : JupyterTestDataAttribute, ITestDataSourceIgnoreCapability
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
            IgnoreMessage = _skipReason;
        }
    }

    internal static string TestConnectionAndReturnSkipReason()
    {
        var connectionString = GetConnectionString();

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return
                $"Environment variable {TEST_DOTNET_JUPYTER_HTTP_CONN} is not set. To run tests that require Jupyter server running, this environment variable must be set to a valid connection string value with --url and --token.";
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
                                             new SimulatedJupyterConnectionOptions(new JupyterHttpKernelConnectionOptions(), KernelSpecName, AllowPlayback),
                                             GetConnectionString());
    }

    /// <summary>
    /// Allows playing back the record of the connection by enabling save to a file 
    /// and reading back from it at the point of save.
    /// </summary>
    public bool AllowPlayback { get; set; }

    public string IgnoreMessage { get; set; }
}