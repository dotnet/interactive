// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Microsoft.DotNet.Interactive.Formatting.Tests.Utility;

public static class StringExtensions
{
    private static readonly Regex _removeStyleElementRegex = new("<style.*</style>", RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Attempts to homogenize an HTML string by reducing whitespace for easier comparison.
    /// </summary>
    /// <param name="s">The string to be crunched.</param>
    public static string Crunch(this string s)
    {
        var result = Regex.Replace(s, "[\n\r]*", ""); // remove newlines
        result = Regex.Replace(result, "\\s*<", "<"); // remove whitespace preceding a tag
        result = Regex.Replace(result, ">\\s*", ">"); // remove whitespace following a tag
        return result;
    }

    public static string IndentHtml(this string html)
    {
        return XElement.Parse(html).ToString();
    }

    public static string RemoveStyleElement(this string html) => 
        _removeStyleElementRegex.Replace(html, "");

    public static string[] SplitIntoLines(this string s) =>
        s.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
}