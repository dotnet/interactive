// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging;

internal static class CSharpLanguageSelector
{
    private static readonly Dictionary<string, string> CSharpLanguageVersions = new(StringComparer.OrdinalIgnoreCase) {
        { "netcoreapp2.0", "7.3" },
        { "netcoreapp2.1", "7.3" },
        { "netstandard2.0", "7.3" },
        { "netcoreapp3.1", "8.0" },
        { "netstandard2.1", "8.0" },
    };

    const string DefaultCSharpLanguageVersion = "7.3";

    public static string GetCSharpLanguageVersion(string targetFramework)
    {
        if (!CSharpLanguageVersions.TryGetValue(targetFramework, out var version))
        {
            version = DefaultCSharpLanguageVersion;
        }

        return version;
    }
}