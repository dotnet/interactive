// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests.Utility;

/// <summary>
/// A Fact attribute that skips the test when running in official/signed Azure Pipelines builds.
/// Official builds are detected by checking if the DOTNET_INTERACTIVE_SIGN_TYPE environment variable is set to "Real",
/// which is set only in the official signed build pipeline (azure-pipelines-official.yml).
/// Tests will still run in public PR builds where DOTNET_INTERACTIVE_SIGN_TYPE is "Test" or not set.
/// </summary>
public sealed class FactSkipCI : FactAttribute
{
    public FactSkipCI(string reason = null)
    {
        var signType = Environment.GetEnvironmentVariable("DOTNET_INTERACTIVE_SIGN_TYPE");
        if (string.Equals(signType, "Real", StringComparison.OrdinalIgnoreCase))
        {
            Skip = string.IsNullOrWhiteSpace(reason) ? "Ignored in official/signed builds" : reason;
        }
    }
}
