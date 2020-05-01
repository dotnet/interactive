// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.DotNet.Interactive
{
    [DebuggerStepThrough]
    internal static class StringExtensions
    {
        public static string TruncateForDisplay(
            this string value,
            int length = 50)
        {
            value = value.Trim();

            if (value.Length > length)
            {
                value = value.Substring(0, length) + " ...";
            }

            return value;
        }
    }
}