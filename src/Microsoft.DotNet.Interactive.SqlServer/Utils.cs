// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.SqlServer
{
    internal static class Utils
    {
        /// <summary>
        /// Returns a version of the string quoted with single quotes. Any single quotes in the string are escaped as ''
        /// </summary>
        /// <param name="str">The string to quote</param>
        /// <returns>The quoted string</returns>
        public static string AsSingleQuotedString(this string str)
        {
            return $"'{str.Replace("'", "''")}'";
        }

        /// <summary>
        /// Returns a version of the string quoted with double quotes. Any double quotes in the string are escaped as \"
        /// </summary>
        /// <param name="str">The string to quote</param>
        /// <returns>The quoted string</returns>
        public static string AsDoubleQuotedString(this string str)
        {
            return $"\"{str.Replace("\"", "\\\"")}\"";
        }
    }
}
