// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Utility
{
    public class ResolvedPackageReference : PackageReference
    {
        public ResolvedPackageReference(
            string packageName,
            string packageVersion,
            IReadOnlyList<FileInfo> assemblyPaths,
            DirectoryInfo packageRoot = null,
            IReadOnlyList<DirectoryInfo> probingPaths = null) : base(packageName, packageVersion)
        {
            if (string.IsNullOrWhiteSpace(packageVersion))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(packageVersion));
            }

            AssemblyPaths = assemblyPaths ?? throw new ArgumentNullException(nameof(assemblyPaths));
            ProbingPaths = probingPaths ?? Array.Empty<DirectoryInfo>();
            PackageRoot = packageRoot ?? AssemblyPaths.FirstOrDefault()?.Directory?.Parent?.Parent;
        }

        public IReadOnlyList<FileInfo> AssemblyPaths { get; }

        public IReadOnlyList<DirectoryInfo> ProbingPaths { get; }

        public DirectoryInfo PackageRoot { get; }

        public override string ToString() => $"{PackageName},{PackageVersion}";
    }
}