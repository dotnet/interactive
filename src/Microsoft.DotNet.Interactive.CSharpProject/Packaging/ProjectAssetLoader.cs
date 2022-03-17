// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging
{
    public class ProjectAssetLoader : IPackageAssetLoader
    {
        public Task<IEnumerable<PackageAsset>> LoadAsync(Package2 package)
        {
            var assets = new List<PackageAsset>();

            var directory = package.DirectoryAccessor;

            foreach (var csproj in directory.GetAllFilesRecursively()
                                            .Where(f => f.Extension == ".csproj"))
            {
                assets.Add(new ProjectAsset(directory.GetDirectoryAccessorForRelativePath(csproj.Directory), csproj.FileName));
            }

            return
                Task.FromResult<IEnumerable<PackageAsset>>(assets);
        }
    }
}