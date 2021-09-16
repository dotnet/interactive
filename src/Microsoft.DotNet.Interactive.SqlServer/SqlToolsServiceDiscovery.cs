// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.DotNet.Interactive.SqlServer
{
    internal static class SqlToolsServiceDiscovery
    {
        public static ResolvedPackageReference FindResolvedPackageReference(this Kernel rootKernel)
        {
            var runtimePackageIdSuffix = "native.Microsoft.SqlToolsService";
            var resolved = rootKernel.SubkernelsAndSelf()
                .OfType<ISupportNuget>()
                .SelectMany(k => k.ResolvedPackageReferences)
                .FirstOrDefault(p => p.PackageName.EndsWith(runtimePackageIdSuffix, StringComparison.OrdinalIgnoreCase));
            return resolved;
        }

        public static string PathToService(this ResolvedPackageReference resolvedPackageReference, string serviceName)
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
