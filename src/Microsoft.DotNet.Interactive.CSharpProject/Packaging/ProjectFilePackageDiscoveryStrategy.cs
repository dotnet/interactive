// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Clockwise;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging
{
    public class ProjectFilePackageDiscoveryStrategy : IPackageDiscoveryStrategy
    {
        private readonly bool _createRebuildablePackage;

        public ProjectFilePackageDiscoveryStrategy(bool createRebuildablePackage)
        {
            _createRebuildablePackage = createRebuildablePackage;
        }

        public Task<PackageBuilder> Locate(
            PackageDescriptor packageDescriptor,
            Budget budget = null)
        {
            var projectFile = packageDescriptor.Name;
            var extension = Path.GetExtension(projectFile);

            if ((extension == ".csproj" || extension == ".fsproj") && File.Exists(projectFile))
            {
                PackageBuilder packageBuilder = new PackageBuilder(packageDescriptor.Name);
                packageBuilder.CreateRebuildablePackage = _createRebuildablePackage;
                packageBuilder.Directory = new DirectoryInfo(Path.GetDirectoryName(projectFile));
                return Task.FromResult(packageBuilder);
            }

            return Task.FromResult<PackageBuilder>(null);
        }
    }
}