// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;    
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Telemetry;
using Pocket;

namespace Microsoft.DotNet.Interactive.App;

public static class KernelExtensionLoader
{
    public static CompositeKernel UseNuGetExtensions(
        this CompositeKernel kernel,
        TelemetrySender telemetrySender = null)
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
                    if (telemetrySender is not null &&
                        packageAdded.PackageReference is { } resolved)
                    {
                        if (resolved.AssemblyPaths.Count == 1)
                        {
                            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(resolved.AssemblyPaths[0]);

                            var logSubscription = LogEvents.Subscribe(
                                e =>
                                {
                                    var eventName = resolved.PackageName + "." + e.OperationName;

                                    Dictionary<string, string> properties = null;

                                    if (e.Properties is { } props)
                                    {
                                        properties = new();

                                        foreach (var tuple in props)
                                        {
                                            properties.TryAdd(tuple.Name, tuple.Value?.ToString());
                                        }
                                    }

                                    telemetrySender.TrackEvent(
                                        eventName,
                                        properties
                                    );
                                },
                                new[] { assembly });

                            kernel.RegisterForDisposal(logSubscription);
                        }
                    }

                    await LoadExtensionsFromDirectoryAsync(
                        kernel,
                        extensionDir,
                        context,
                        telemetrySender);
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

    public static async Task LoadExtensionsFromDirectoryAsync(
        this CompositeKernel kernel,
        DirectoryInfo extensionDir,
        KernelInvocationContext context,
        TelemetrySender telemetrySender = null)
    {
        await new PackageDirectoryExtensionLoader(telemetrySender).LoadFromDirectoryAsync(
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