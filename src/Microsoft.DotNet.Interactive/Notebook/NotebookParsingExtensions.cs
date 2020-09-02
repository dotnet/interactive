// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Notebook
{
    internal static class NotebookParsingExtensions
    {
        public static IEnumerable<string> SplitAsLines(string s)
        {
            return s.Split('\n').Select(l => l.EndsWith('\r') ? l[0..^1] : l);
        }

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
