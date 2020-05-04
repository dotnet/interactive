// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public static class StringExtensions
    {
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

        /// <summary>
        /// Attempts to homogenize an HTML string by reducing whitespace for easier comparison.
        /// </summary>
        /// <param name="s">The string to be crunched.</param>
        public static string Crunch(this IHtmlContent s)
        {
            return s.ToString().Crunch();
        }
    }
}