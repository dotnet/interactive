// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging;

internal class DirectoryPackageDiscoveryStrategy : IPackageDiscoveryStrategy
{
    private readonly bool _createRebuildablePackage;

    public DirectoryPackageDiscoveryStrategy(bool createRebuildablePackage)
    {
        _createRebuildablePackage = createRebuildablePackage;
    }

    public Task<PackageBuilder> LocatePackageAsync(PackageDescriptor packageDescriptor)
    {
        var directory = new DirectoryInfo(Path.Combine(
            Package.DefaultPackagesDirectory.FullName, packageDescriptor.Name));

        if (directory.Exists)
        {
            var packageBuilder = new PackageBuilder(packageDescriptor.Name);
            packageBuilder.CreateRebuildablePackage = _createRebuildablePackage;
            return Task.FromResult(packageBuilder);
        }

        return Task.FromResult<PackageBuilder>(null);
    }
}