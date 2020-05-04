// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Management.Automation.Host;
using System.Threading;

namespace Microsoft.DotNet.Interactive.PowerShell.Host
{
    internal static class StringUtil
    {
        internal static string Format(string formatSpec, object arg)
        {
            return string.Format(CultureInfo.CurrentCulture, formatSpec, arg);
        }

        internal static string Format(string formatSpec, object arg1, object arg2)
        {
            return string.Format(CultureInfo.CurrentCulture, formatSpec, arg1, arg2);
        }

        internal static string Format(string formatSpec, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, formatSpec, args);
        }

        internal static string TruncateToBufferCellWidth(PSHostRawUserInterface rawUI, string toTruncate, int maxWidthInBufferCells)
        {
            string result;
            int i = Math.Min(toTruncate.Length, maxWidthInBufferCells);

            do
            {
                result = toTruncate.Substring(0, i);
                int cellCount = rawUI.LengthInBufferCells(result);
                if (cellCount <= maxWidthInBufferCells)
                {
                    // The segment from start..i fits.
                    break;
                }
                else
                {
                    // The segment does not fit, back off a tad until it does
                    // We need to back off 1 by 1 because there could theoretically
                    // be characters taking more 2 buffer cells
                    i--;
                }
            } while (true);

            return result;
        }

        private const int IndentCacheMax = 120;
        private static readonly string[] IndentCache = new string[IndentCacheMax];

        /// <summary>
        /// Typical padding is at most a screen's width.
        /// We assume the max width is 120, and we won't bother caching any more than that.
        /// </summary>
        internal static string Padding(int countOfSpaces)
        {
            if (countOfSpaces >= IndentCacheMax)
            {
                return new string(' ', countOfSpaces);
            }

            var result = IndentCache[countOfSpaces];
            if (result == null)
            {
                Interlocked.CompareExchange(ref IndentCache[countOfSpaces], new string(' ', countOfSpaces), null);
                result = IndentCache[countOfSpaces];
            }

            return result;
        }
    }
}
