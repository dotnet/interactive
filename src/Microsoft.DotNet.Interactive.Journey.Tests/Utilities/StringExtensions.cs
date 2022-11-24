// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Journey.Tests.Utilities;

public static class StringExtensions
{
    public static string Join(this IEnumerable<string> s, string delimiter)
    {
        return string.Join(delimiter, s);
    }

    public static bool ContainsAll(this string s, params string[] expectedSubstrings)
    {
        foreach (var substring in expectedSubstrings)
        {
            if (!s.Contains(substring))
            {
                return false;
            }
        }
        return true;
    }
}