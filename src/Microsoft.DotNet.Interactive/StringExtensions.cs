// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.DotNet.Interactive;

[DebuggerStepThrough]
internal static class StringExtensions
{
    public static string TruncateForDisplay(
        this string value,
        int length = 50)
    {
        value = value.Trim();
        var lines = value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var firstLine = lines.FirstOrDefault();

        if (string.IsNullOrEmpty(firstLine))
        {
            return string.Empty;
        }
        else
        {
            if (firstLine.Length > length)
            {
                firstLine = firstLine[..length] + " ...";
            }
            else if (lines.Length > 1)
            {
                firstLine = firstLine + " ...";
            }

            return firstLine;
        }
    }
}