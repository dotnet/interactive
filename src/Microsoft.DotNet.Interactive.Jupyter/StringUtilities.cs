// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Interactive.Jupyter;

internal static class StringUtilities
{
    public static string NormalizeLineEndings(this string source)
    {
        return source.Replace("\r\n", "\n");
    }

    public static string StripUnsupportedTextFormats(this string source)
    {
        // strip away ansi color formats
        string unsupportedFormats = "\u001b\\[.*?m|_\b";
        return Regex.Replace(source, unsupportedFormats, string.Empty);
    }
}
