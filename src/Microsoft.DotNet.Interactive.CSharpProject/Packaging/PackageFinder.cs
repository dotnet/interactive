// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging
{
    public static class PackageFinder
    {
        public static Task<T> FindAsync<T>(
            this IPackageFinder finder,
            string packageName)
            where T : class, IPackage =>
            finder.Find<T>(new PackageDescriptor(packageName));

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

            public async Task<T> Find<T>(PackageDescriptor descriptor) where T : class, IPackage
            {
                var package = await _lazyPackage.ValueAsync();

                if (package is T p)
                {
                    return p;
                }

                return default;
            }
        }
    }
}