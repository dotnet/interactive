// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Microsoft.DotNet.Interactive.PackageManagement")]

namespace Microsoft.DotNet.Interactive;

public class ResolvedPackageReference : PackageReference
{
    public ResolvedPackageReference(
        string packageName,
        string packageVersion,
        IReadOnlyList<string> assemblyPaths,
        string packageRoot = null,
        IReadOnlyList<string> probingPaths = null) : base(packageName, packageVersion)
    {
        if (string.IsNullOrWhiteSpace(packageVersion))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(packageVersion));
        }

        AssemblyPaths = assemblyPaths ?? throw new ArgumentNullException(nameof(assemblyPaths));
        ProbingPaths = probingPaths ?? Array.Empty<string>();
        PackageRoot = packageRoot;

        if (PackageRoot is null && 
            AssemblyPaths.FirstOrDefault() is {} path)
        {
            PackageRoot = new FileInfo(path).Directory?.Parent?.Parent?.FullName;
        }
    }

    public IReadOnlyList<string> AssemblyPaths { get; }

    public IReadOnlyList<string> ProbingPaths { get; }

    public string PackageRoot { get; }

    public override string ToString() => $"{PackageName},{PackageVersion}";
}