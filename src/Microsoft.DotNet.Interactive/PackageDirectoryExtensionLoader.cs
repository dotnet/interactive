﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;
using Pocket;
using static Pocket.Logger<Microsoft.DotNet.Interactive.PackageDirectoryExtensionLoader>;

namespace Microsoft.DotNet.Interactive;

internal class PackageDirectoryExtensionLoader : IKernelExtensionLoader
{
    private const string ExtensionScriptName = "extension.dib";

    private readonly HashSet<AssemblyName> _loadedAssemblies = new();
    private readonly object _lock = new();

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

        using var op = new ConfirmationLogger(
            Log.Category,
            message: $"Loading extensions in directory {directory}",
            logOnStart: true,
            args: new object[] { directory });

        await LoadFromDllsInDirectory(
            directory,
            kernel,
            context);

        await LoadFromScript(
            directory,
            kernel,
            context);

        op.Succeed();
    }

    public async Task LoadFromDllsInDirectory(
        DirectoryInfo directory,
        Kernel kernel,
        KernelInvocationContext context)
    {
        var extensionDlls = directory.GetFiles("*.dll", SearchOption.TopDirectoryOnly);

        foreach (var extensionDll in extensionDlls)
        {
            await LoadFromAssemblyFile(
                extensionDll,
                kernel,
                context);
        }
    }

    private async Task LoadFromAssemblyFile(
        FileInfo assemblyFile,
        Kernel kernel,
        KernelInvocationContext context)
    {
        bool loadExtensions;

        lock (_lock)
        {
            loadExtensions = _loadedAssemblies.Add(AssemblyName.GetAssemblyName(assemblyFile.FullName));
        }

        if (loadExtensions)
        {
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyFile.FullName);

            await LoadFromAssembly(assembly, kernel, context);
        }
    }

    private static async Task LoadFromAssembly(
        Assembly assembly,
        Kernel kernel,
        KernelInvocationContext context)
    {
        var extensionTypes = assembly
                             .ExportedTypes
                             .Where(t => t.CanBeInstantiated() && typeof(IKernelExtension).IsAssignableFrom(t))
                             .ToArray();

        if (extensionTypes.Any())
        {
            context.Display($"Loading extensions from `{assembly.Location}`");
        }

        foreach (var extensionType in extensionTypes)
        {
            var extension = (IKernelExtension)Activator.CreateInstance(extensionType);

            try
            {
                await extension.OnLoadAsync(kernel);
                context.Publish(new KernelExtensionLoaded(extension, context.Command));
            }
            catch (Exception e)
            {
                context.Publish(new ErrorProduced(
                                    $"Failed to load kernel extension \"{extensionType.Name}\" from assembly {assembly.Location}",
                                    context.Command));

                context.Fail(context.Command, new KernelExtensionLoadException(e));
            }
        }
    }

    private async Task LoadFromScript(
        DirectoryInfo directory,
        Kernel kernel,
        KernelInvocationContext context)
    {
        var extensionFile = new FileInfo(Path.Combine(directory.FullName, ExtensionScriptName));

        if (extensionFile.Exists)
        {
            var logMessage = $"Loading extension script from `{extensionFile.FullName}`";

            using var op = new ConfirmationLogger(
                Log.Category,
                message: logMessage,
                logOnStart: true,
                args: new object[] { extensionFile });

            context.Display(logMessage);

            var scriptContents = await File.ReadAllTextAsync(extensionFile.FullName, Encoding.UTF8);

            await kernel.SubmitCodeAsync(scriptContents);
        }
    }
}