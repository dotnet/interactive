// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.DependencyManager;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive
{
    public interface ISupportNuget : IPackageRestoreContext
    {
        // KernelSupportsNugetExtension relies on access to the full PackageRestoreContextClass for state mutation
        PackageRestoreContext PackageRestoreContext { get; }

        // Notifies Kernel that packagereferencing is complete, and provides a list of PackageReferences
        void RegisterResolvedPackageReferences(IReadOnlyList<ResolvedPackageReference> packageReferences);
    }
}