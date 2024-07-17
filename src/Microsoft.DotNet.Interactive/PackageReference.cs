// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Microsoft.DotNet.Interactive;

public class PackageReference
{
    public PackageReference(string packageName, string packageVersion = null)
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(packageName));
        }

        PackageName = packageName ;
        PackageVersion = packageVersion ?? string.Empty;
    }

    public string PackageName { get; }

    public string PackageVersion { get; }

    public static bool TryParse(string value, out PackageReference reference)
    {
        value = value.Trim([' ', '\t', '"']);

        if (!value.StartsWith("nuget:"))
        {
            reference = null;
            return false;
        }
        var parts = value.Split([','], 2)
            .Select(v => v.Trim())
            .ToArray();

        if (parts.Length is 0)
        {
            reference = null;
            return false;
        }

        var packageName = parts[0][6..].Trim();

        if (string.IsNullOrWhiteSpace(packageName))
        {
            reference = null;
            return false;
        }

        var packageVersion = parts.Length > 1
            ? parts[1]
            : null;

        reference = new PackageReference(packageName, packageVersion);

        return true;
    }

    public bool IsPackageVersionSpecified => !string.IsNullOrWhiteSpace(PackageVersion) || PackageVersion is "*";

    public override string ToString()
    {
        if (string.IsNullOrWhiteSpace(PackageVersion))
        {
            return PackageName;
        }

        return $"{PackageName},{PackageVersion}";
    }
}