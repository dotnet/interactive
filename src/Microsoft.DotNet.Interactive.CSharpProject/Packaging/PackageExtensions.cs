// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Pocket;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging
{
    internal static class PackageExtensions
    {
        public static async Task<bool> Create(
            this IHaveADirectory packageBase,
            IPackageInitializer initializer)
        {
            using (var operation = Logger<PackageBase>.Log.OnEnterAndConfirmOnExit())
            {
                if (!packageBase.Directory.Exists)
                {

                    operation.Info("Creating directory {directory}", packageBase.Directory);
                    packageBase.Directory.Create();
                    packageBase.Directory.Refresh();
                }

                using (await FileLock.TryCreateAsync(packageBase.Directory))
                {
                    if (!packageBase.Directory.GetFiles("*", SearchOption.AllDirectories).Where(f => !FileLock.IsLockFile(f)).Any())
                    {
                        operation.Info("Initializing package using {_initializer} in {directory}", initializer,
                            packageBase.Directory);
                        await initializer.InitializeAsync(packageBase.Directory);
                    }
                }

                operation.Succeed();
                return true;
            }
        }
    }
}