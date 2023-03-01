// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Packaging;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class FindPackageInDefaultLocation : IPackageFinder
{
    private readonly IDirectoryAccessor _directoryAccessor;

    public FindPackageInDefaultLocation(IDirectoryAccessor directoryAccessor = null)
    {
        _directoryAccessor = directoryAccessor ??
                             new FileSystemDirectoryAccessor(Package.DefaultPackagesDirectory);
    }

    public async Task<T> Find<T>(PackageDescriptor descriptor)
        where T : class, IPackage
    {
        if (!descriptor.IsPathSpecified)
        {

            if (_directoryAccessor.DirectoryExists(descriptor.Name))
            {
                var directoryAccessor = _directoryAccessor.GetDirectoryAccessorForRelativePath(descriptor.Name);

                var pkg = new Package2(
                    descriptor,
                    directoryAccessor);

                pkg.Add(new ProjectAsset(directoryAccessor));

                if (pkg is T t)
                {
                    return t;
                }
            }
        }

        return default;
    }
}