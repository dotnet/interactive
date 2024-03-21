// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging;

public static class PackageFinder
{
    public static Task<Package> FindAsync(
        this IPackageFinder finder,
        string packageName) =>
        finder.FindAsync(new PackageDescriptor(packageName));

    public static IPackageFinder Create(Func<Task<Package>> package)
    {
        return new AnonymousPackageFinder(package);
    }

    private class AnonymousPackageFinder : IPackageFinder
    {
        private readonly AsyncLazy<Package> _lazyPackage;

        public AnonymousPackageFinder(Func<Task<Package>> package)
        {
            _lazyPackage = new(package);
        }

        public async Task<Package> FindAsync(PackageDescriptor descriptor)
        {
            return await _lazyPackage.ValueAsync();
        }
    }
}