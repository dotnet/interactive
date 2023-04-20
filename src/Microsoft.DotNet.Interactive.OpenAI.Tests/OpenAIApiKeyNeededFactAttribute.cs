// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.DotNet.Interactive.OpenAI.Tests;

public sealed class AIIngregrationTestFactAttribute : FactAttribute
{
    private const string DOTNET_INTERACTIVE_AI_CONFIG_FILE_PATH = nameof(DOTNET_INTERACTIVE_AI_CONFIG_FILE_PATH);
    private static readonly string _skipReason;

    static AIIngregrationTestFactAttribute()
    {
        _skipReason = TestConnectionAndReturnSkipReason();
    }

    public AIIngregrationTestFactAttribute()
    {
        if (_skipReason is not null)
        {
            Skip = _skipReason;
        }
    }

    internal static string TestConnectionAndReturnSkipReason()
    {
        string configFilePath = ConfigFilePath();

        if (string.IsNullOrWhiteSpace(configFilePath))
        {
            return
                $"Environment variable {DOTNET_INTERACTIVE_AI_CONFIG_FILE_PATH} is not set. To run tests that require AI services, this environment variable must be set to a valid config file path.";
        }

        if (!File.Exists(configFilePath))
        {
            return $"Config file specified by environment variable {DOTNET_INTERACTIVE_AI_CONFIG_FILE_PATH} not found at path {configFilePath}";
        }

        return null;
    }

    public static string ConfigFilePath()
    {
        return Environment.GetEnvironmentVariable(DOTNET_INTERACTIVE_AI_CONFIG_FILE_PATH);
    }
}