// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Documents;

public static class StringExtensions
{
    internal static string[] SplitIntoLines(this string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return Array.Empty<string>();
        }

        return s.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
    }

    public static IEnumerable<string>  SplitIntoLines2(this string  value)
    {


        foreach (var c in value)
        {
            






        }


        return value;
    }

    internal static string TrimNewline(this string s)
    {
        if (s.EndsWith("\r\n"))
        {
            return s[0..^2];
        }

        if (s.EndsWith("\n"))
        {
            return s[0..^1];
        }

        return s;
    }

    private static string[] RemoveLastElementIfEmpty(this string[] values)
    {
        if (values.Length > 0 && values[^1] == "")
        {
            return values[..^1];
        }

        return values;
    }

    internal static string[] EnsureTrailingNewlinesOnAllButLast(this string[] lines)
    {
        var result = lines
                     .RemoveLastElementIfEmpty()
                     .Select(l => l.EndsWith("\n")
                                      ? l
                                      : l + "\n")
                     .ToArray();

        if (result.Length > 0)
        {
            result[^1] = lines[^1];
        }

        return result;
    }
}