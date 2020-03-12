// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Interactive.DependencyManager;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive
{
    public interface ISupportNuget
    {
        // Summary:
        //     Construct if needed and register new DependencyProvider
        public abstract void InitializeDependencyProvider(AssemblyResolutionProbe assemblyProbingPaths = null, NativeResolutionProbe nativeProbingRoots = null);

        public abstract void RegisterNugetResolvedPackageReferences(IReadOnlyList<ResolvedPackageReference> packageReferences);

        // Summary:
        //     Resolve reference for a list of package manager lines
        public abstract IResolveDependenciesResult Resolve(IEnumerable<string> packageManagerTextLines, string executionTfm);
    }
}