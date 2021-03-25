﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests
{
    internal static class StringExtensions
    {
        public static string FixedGuid(this string source)
        {
            var reg = new Regex(@".*\s+id=""(?<id>\S+)""\s+.*", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
            var id1 = reg.Match(source).Groups["id"].Value;
            var id = id1;
            return source.Replace(id, "00000000000000000000000000000000");
        }
    }
}