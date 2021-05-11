// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Interactive
{
    public class ResolvedPackageReference : PackageReference
    {
        public ResolvedPackageReference(
            string packageName,
            string packageVersion,
            bool requested,
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
            Requested = requested;
        }

        public IReadOnlyList<string> AssemblyPaths { get; }

        public IReadOnlyList<string> ProbingPaths { get; }

        public string PackageRoot { get; }
        internal bool Requested { get; }

        public override string ToString() => $"{PackageName},{PackageVersion}";
    }
}