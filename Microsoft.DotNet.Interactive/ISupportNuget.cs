// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive
{
    public interface ISupportNuget
    {
        public abstract void RegisterNugetResolvedPackageReferences(IReadOnlyList<ResolvedPackageReference> packageReferences);

        public abstract string ScriptExtension { get; }
    }
}