// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Interactive.Mermaid.Tests;

internal static class StringExtensions
{
    public static string FixedGuid(this string source)
    {
        var reg = new Regex(@".*\s+id=""(?<id>\S+)""\s*.*", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        var id1 = reg.Match(source).Groups["id"].Value;
        var id = id1;
        return string.IsNullOrWhiteSpace(id) ? source : source.Replace(id, "00000000000000000000000000000000");
    }

    public static string FixedCacheBuster(this string source)
    {
        var reg = new Regex(@".*\s+'cacheBuster=(?<cacheBuster>\S+)'\s*.*", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        var id1 = reg.Match(source).Groups["cacheBuster"].Value;
        var id = id1;
        return string.IsNullOrWhiteSpace(id) ? source : source.Replace(id, "00000000000000000000000000000000");
    }
}