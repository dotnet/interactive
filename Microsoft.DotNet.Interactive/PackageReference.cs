// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Interactive
{
    public class PackageReference
    {
        private static readonly Regex _regex = new Regex(
            @"(?<moniker>nuget)(?<colon>\s*:\s*)(?<packageName>(?!RestoreSources\s*=\s*)[^,]+)*(\s*,\s*(?<packageVersion>(?!RestoreSources\s*=\s*)[^,]+))*(?<comma>\s*,\s*)*(RestoreSources\s*=\s*(?<restoreSources>[^,]+)*)*",
            RegexOptions.Compiled |
            RegexOptions.CultureInvariant |
            RegexOptions.Singleline);

        public PackageReference(string packageName, string packageVersion = null, string restoreSources = null)
        {
            PackageName = packageName ?? string.Empty;
            PackageVersion = packageVersion ?? string.Empty;
            RestoreSources = restoreSources ?? string.Empty;
        }

        public string PackageName { get; }

        public string PackageVersion { get; }

        public string RestoreSources { get; }

        public static bool TryParse(string value, out PackageReference reference)
        {
            var result = _regex.Match(value);

            if (!result.Success)
            {
                reference = null;
                return false;
            }

            var packageName = result.Groups["packageName"].Value;
            var packageVersion = result.Groups["packageVersion"].Value;
            var restoreSources = result.Groups["restoreSources"].Value;
            reference = new PackageReference(packageName, packageVersion, restoreSources);

            return true;
        }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(PackageVersion))
            {
                return PackageName;
            }

            return $"{PackageName},{PackageVersion}";
        }
    }
}