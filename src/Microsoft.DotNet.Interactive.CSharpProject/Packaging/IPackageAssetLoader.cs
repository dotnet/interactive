// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging
{
    public interface IPackageAssetLoader
    {
        Task<IEnumerable<PackageAsset>> LoadAsync(Package2 package);
    }
}