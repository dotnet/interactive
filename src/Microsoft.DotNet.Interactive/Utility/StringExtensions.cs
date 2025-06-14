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
        int maxLength = 50)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var lines = value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var firstLine = lines.FirstOrDefault();

        if (string.IsNullOrEmpty(firstLine))
        {
            return string.Empty;
        }

        if (firstLine.Length > maxLength)
        {
            firstLine = firstLine[..maxLength] + " ...";
        }
        else if (lines.Length > 1)
        {
            firstLine += " ...";
        }

        return firstLine;
    }
}
