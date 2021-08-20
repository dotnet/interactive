// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Documents
{
    internal static class StringExtensions
    {
        public static string[] SplitAsLines(string s) => 
            s.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        public static string TrimNewline(this string s)
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
    }
}