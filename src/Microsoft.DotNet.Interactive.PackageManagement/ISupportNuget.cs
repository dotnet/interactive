// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive;

public interface ISupportNuget
{
    Task<PackageRestoreResult> RestoreAsync();

    IEnumerable<string> RestoreSources { get; }

    IEnumerable<PackageReference> RequestedPackageReferences { get; }

    IEnumerable<ResolvedPackageReference> ResolvedPackageReferences { get; }

    void Configure(bool useResultsCache);

    PackageReference GetOrAddPackageReference(
        string packageName,
        string packageVersion = null);

    void TryAddRestoreSource(string source);

    // Notifies Kernel that package referencing is complete, and provides a list of PackageReferences
    void RegisterResolvedPackageReferences(IReadOnlyList<ResolvedPackageReference> packageReferences);
}