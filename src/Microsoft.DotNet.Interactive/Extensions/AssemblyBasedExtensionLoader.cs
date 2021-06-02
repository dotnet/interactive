// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;
using Pocket;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Extensions.AssemblyBasedExtensionLoader>;

namespace Microsoft.DotNet.Interactive.Extensions
{
    public class AssemblyBasedExtensionLoader : IKernelExtensionLoader
    {
        private readonly HashSet<AssemblyName> _loadedAssemblies = new HashSet<AssemblyName>();
        private readonly object _lock = new object();

        public async Task LoadFromDirectoryAsync(
            DirectoryInfo directory,
            Kernel kernel,
            KernelInvocationContext context)
        {
            if (directory is null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (kernel is null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!directory.Exists)
            {
                throw new ArgumentException($"Directory {directory.FullName} doesn't exist", nameof(directory));
            }

            await LoadFromAssembliesInDirectory(
                directory,
                kernel,
                context);
        }

        public async Task LoadFromAssembliesInDirectory(
            DirectoryInfo directory,
            Kernel kernel,
            KernelInvocationContext context)
        {
            if (directory.Exists)
            {
                var extensionDlls = directory
                                    .GetFiles("*.dll", SearchOption.TopDirectoryOnly)
                                    .ToList();

                if (extensionDlls.Count > 0)
                {
                    using var op = new ConfirmationLogger(
                        Log.Category,
                        message: $"Loading extensions in directory {directory}",
                        logOnStart: true,
                        args: new object[] { directory });

                    foreach (var extensionDll in extensionDlls)
                    {
                        await LoadFromAssembly(
                            extensionDll,
                            kernel,
                            context);
                    }

                    op.Succeed();
                }
            }
        }

        private async Task LoadFromAssembly(
            FileInfo assemblyFile,
            Kernel kernel,
            KernelInvocationContext context)
        {
            if (assemblyFile is null)
            {
                throw new ArgumentNullException(nameof(assemblyFile));
            }

            if (kernel is null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (!assemblyFile.Exists)
            {
                throw new ArgumentException($"File {assemblyFile.FullName} doesn't exist", nameof(assemblyFile));
            }

            bool loadExtensions;

            lock (_lock)
            {
                loadExtensions = _loadedAssemblies.Add(AssemblyName.GetAssemblyName(assemblyFile.FullName));
            }

            if (loadExtensions)
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyFile.FullName);

                var extensionTypes = assembly
                                     .ExportedTypes
                                     .Where(t => t.CanBeInstantiated() && typeof(IKernelExtension).IsAssignableFrom(t))
                                     .ToArray();

                if (extensionTypes.Any())
                {
                    context.Display($"Loading extensions from `{assemblyFile.Name}`", "text/markdown");
                }

                foreach (var extensionType in extensionTypes)
                {
                    var extension = (IKernelExtension) Activator.CreateInstance(extensionType);

                    try
                    {
                        await extension.OnLoadAsync(kernel);
                        context.Publish(new KernelExtensionLoaded(extension, context.Command));
                    }
                    catch (Exception e)
                    {
                        context.Publish(new ErrorProduced(
                                            $"Failed to load kernel extension \"{extensionType.Name}\" from assembly {assemblyFile.FullName}",
                                            context.Command));

                        context.Fail(new KernelExtensionLoadException(e));
                    }
                }
            }
        }
    }
}