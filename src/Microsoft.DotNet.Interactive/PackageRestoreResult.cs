// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive
{
    public class PackageRestoreResult
    {
        public PackageRestoreResult(
            bool succeeded,
            IEnumerable<PackageReference> requestedPackages,
            IReadOnlyList<ResolvedPackageReference> resolvedReferences = null,
            IReadOnlyCollection<string> errors = null)
        {
            if (requestedPackages == null)
            {
                throw new ArgumentNullException(nameof(requestedPackages));
            }

            if (!succeeded && errors?.Count == 0)
            {
                throw new ArgumentException("Must provide errors when succeeded is false.");                //TBD: Localize
            }

            Succeeded = succeeded;

            ResolvedReferences = resolvedReferences ?? Array.Empty<ResolvedPackageReference>();

            Errors = errors ?? Array.Empty<string>();
        }

        public IReadOnlyList<ResolvedPackageReference> ResolvedReferences { get; }

        public IReadOnlyList<DirectoryInfo> NativeLibraryProbingPaths =>
            ResolvedReferences
                .SelectMany(path => path.ProbingPaths)
                .Distinct()
                .ToArray();

        public bool Succeeded { get; }

        public IReadOnlyCollection<string> Errors { get; }
    }
}