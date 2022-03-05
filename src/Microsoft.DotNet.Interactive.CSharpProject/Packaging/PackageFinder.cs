// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Clockwise;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging
{
    public static class PackageFinder
    {
        public static Task<T> Find<T>(
            this IPackageFinder finder, 
            string packageName, 
            Budget budget = null) 
            where T : class, IPackage =>
            finder.Find<T>(new PackageDescriptor(packageName));

        public static IPackageFinder Create(IPackage package)
        {
            return new AnonymousPackageFinder(package);
        }

        private class AnonymousPackageFinder : IPackageFinder
        {
            private readonly IPackage _package;

            public AnonymousPackageFinder(IPackage package)
            {
                _package = package ?? throw new ArgumentNullException(nameof(package));
            }

            public Task<T> Find<T>(PackageDescriptor descriptor) where T : class, IPackage
            {
                if (_package is T package)
                {
                    return Task.FromResult(package);
                }

                return default;
            }
        }
    }
}