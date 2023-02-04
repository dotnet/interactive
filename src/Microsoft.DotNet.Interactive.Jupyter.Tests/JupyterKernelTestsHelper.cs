// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

internal sealed class JupyterZMQConnectionHelper 
{
    public const string TEST_DOTNET_JUPYTER_ZMQ_CONN = nameof(TEST_DOTNET_JUPYTER_ZMQ_CONN);
    public static readonly string SkipReason;
    
    static JupyterZMQConnectionHelper()
    {
        SkipReason = TestConnectionAndReturnSkipReason();
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

internal sealed class JupyterHttpConnectionHelper
{
    public const string TEST_DOTNET_JUPYTER_HTTP_CONN = nameof(TEST_DOTNET_JUPYTER_HTTP_CONN);
    public static readonly string SkipReason;

    static JupyterHttpConnectionHelper()
    {
        SkipReason = TestConnectionAndReturnSkipReason();
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


internal class JupyterKernelTestHelper
{
    public static TestJupyterConnectionOptions GetConnectionOptions(Type connectionOptionsToTest = null)
    {
        var options = new TestJupyterConnectionOptions();
        if (connectionOptionsToTest == typeof(JupyterHttpKernelConnectionOptions) && JupyterHttpConnectionHelper.SkipReason is null)
        {
            options.Record(new JupyterHttpKernelConnectionOptions(), JupyterHttpConnectionHelper.GetConnectionString());
        }
        else if (connectionOptionsToTest == typeof(JupyterLocalKernelConnectionOptions) && JupyterZMQConnectionHelper.SkipReason is null)
        {
            options.Record(new JupyterLocalKernelConnectionOptions());
        }
        else if (connectionOptionsToTest == null)
        {
            options.Playback(null);
        }

        return options;
    }
}
