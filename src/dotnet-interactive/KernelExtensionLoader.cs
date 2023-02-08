// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.App;

public static class KernelExtensionLoader
{
    public static CompositeKernel UseNuGetExtensions(this CompositeKernel kernel)
    {
        var packagesToCheckForExtensions = new ConcurrentQueue<PackageAdded>();

        kernel.AddMiddleware(async (command, context, next) =>
        {
            await next(command, context);

            while (packagesToCheckForExtensions.TryDequeue(out var packageAdded))
            {
                var packageRootDir = packageAdded.PackageReference.PackageRoot;

                var extensionDir =
                    new DirectoryInfo
                    (Path.Combine(
                         packageRootDir,
                         "interactive-extensions",
                         "dotnet"));

                if (extensionDir.Exists)
                {
                    await LoadExtensionsFromDirectoryAsync(kernel, extensionDir, context);
                }
            }
        });

        kernel.RegisterForDisposal(
            kernel.KernelEvents
                  .OfType<PackageAdded>()
                  .Where(pa => pa?.PackageReference.PackageRoot is not null)
                  .Distinct(pa => pa.PackageReference.PackageRoot)
                  .Subscribe(added => packagesToCheckForExtensions.Enqueue(added)));

        return kernel;
    }

    public static async Task LoadExtensionsFromDirectoryAsync(this CompositeKernel kernel, DirectoryInfo extensionDir, KernelInvocationContext context)
    {
        await new PackageDirectoryExtensionLoader().LoadFromDirectoryAsync(
            extensionDir,
            kernel,
            context);
    }

    internal static bool CanBeInstantiated(this Type type)
    {
        return !type.IsAbstract
               && !type.IsGenericTypeDefinition
               && !type.IsInterface;
    }
}