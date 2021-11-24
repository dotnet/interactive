// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Interactive;

namespace Microsoft.DotNet.Interactive.SqlServer
{
    internal static class SqlToolsServiceDiscovery
    {
        /// <summary>
        /// Finds the resolved native package for the Microsoft.SqlToolsService package which contains the SQL Tools Service binaries. This may fail to find
        /// a resolved package reference if the platform RID isn't supported by SQL Tools Service.
        /// </summary>
        /// <param name="rootKernel">The root kernel to look at the package references of</param>
        /// <returns>The package reference if a matching one exists, null if not</returns>
        public static ResolvedPackageReference FindResolvedNativeSqlToolsServicePackageReference(this Kernel rootKernel)
        {
            var runtimePackageIdSuffix = "native.Microsoft.SqlToolsService";
            var resolved = rootKernel.SubkernelsAndSelf()
                .OfType<ISupportNuget>()
                .SelectMany(k => k.ResolvedPackageReferences)
                .FirstOrDefault(p => p.PackageName.EndsWith(runtimePackageIdSuffix, StringComparison.OrdinalIgnoreCase));
            return resolved;
        }

        /// <summary>
        /// Generates the full path to the service of the name specified from within this resolved package reference.
        /// </summary>
        /// <param name="resolvedPackageReference">The package containing the Tools Service binaries</param>
        /// <param name="serviceName">The name of the exe to search for (without the extension)</param>
        /// <returns></returns>
        public static string PathToToolsService(this ResolvedPackageReference resolvedPackageReference, string serviceName)
        {
            var pathToService = "";
            if (resolvedPackageReference is not null)
            {
                // Extract the platform 'osx-x64' from the package name 'runtime.osx-x64.native.microsoft.sqltoolsservice'
                var packageNameSegments = resolvedPackageReference.PackageName.Split(".");
                if (packageNameSegments.Length > 2)
                {
                    var platform = packageNameSegments[1];

                    // Build the path to the MicrosoftSqlToolsServiceLayer executable by reaching into the resolve nuget package
                    // assuming a convention for native binaries.
                    pathToService = Path.Combine(
                        resolvedPackageReference.PackageRoot,
                        "runtimes",
                        platform,
                        "native",
                        serviceName);

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        pathToService += ".exe";
                    }
                }
            }

            return pathToService;
        }
    }
}
