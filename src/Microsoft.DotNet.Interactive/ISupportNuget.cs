// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.DependencyManager;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive
{
    public interface ISupportNuget
    {
        // Set assemblyProbingPaths, nativeProbingRoots for Kernel.
        // These values are functions that return the list of discovered assemblies, and package roots
        // They are used by the dependecymanager for Assembly and Native dll resolving
        public void Initialize(AssemblyResolutionProbe assemblyProbingPaths, NativeResolutionProbe nativeProbingRoots);

        public void RegisterResolvedPackageReferences(IReadOnlyList<ResolvedPackageReference> packageReferences);

        // Summary:
        //     Resolve reference for a list of package manager lines
        public IResolveDependenciesResult Resolve(IEnumerable<string> packageManagerTextLines, string executionTfm, ResolvingErrorReport reportError);
    }
}