// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive
{
    public class AddNugetRestoreSourcesResult : AddNugetResult
    {
        public AddNugetRestoreSourcesResult(
            bool succeeded,
            PackageReference requestedPackage,
            IReadOnlyList<ResolvedPackageReference> addedReferences = null,
            IReadOnlyCollection<string> errors = null) : base(succeeded, requestedPackage, errors)
        {
        }
    }
}