// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Documents.Utility;

internal static class StringExtensions
{
    internal static string[] SplitIntoLines(this string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return Array.Empty<string>();
        }

        return s.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
    }

    internal static string[] SplitIntoJupyterFileArray(this string value)
    {
        var lines = new List<string>();

        int startLineIndex = 0;

        // each substring ending in \n is its own array item
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == '\n')
            {
                lines.Add(value[startLineIndex..(i + 1)]);
                startLineIndex = i + 1;
            }
        }

        // the remainder of the string, if any, is an additional array item
        if (startLineIndex < value.Length)
        {
            lines.Add(value[startLineIndex..]);
        }

        return lines.ToArray();
    }
}