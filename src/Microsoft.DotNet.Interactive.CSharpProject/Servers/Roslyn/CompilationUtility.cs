// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Packaging;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;

internal static class CompilationUtility
{
    internal static bool CanBeUsedToGenerateCompilation(this CodeAnalysis.Workspace workspace)
    {
        return workspace?.CurrentSolution?.Projects?.Count() > 0;
    }

    public static async Task WaitForFileAvailable(
        this FileInfo file)
    {
        if (file is null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        const int waitAmount = 100;
        var attemptCount = 1;
        while (file.Exists && attemptCount <= 10 && !IsAvailable())
        {
            await Task.Delay(waitAmount * attemptCount);
            attemptCount++;
        }

        bool IsAvailable()
        {
            try
            {
                using (file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }
    }

    public static FileInfo GetProjectFile(this Package package) =>
        package.Directory.GetFiles("*.csproj").FirstOrDefault();

    public static void CleanObjFolder(this Package package)
    {
        var targets = package.Directory.GetDirectories("obj");
        foreach (var target in targets)
        {
            target.Delete(true);
        }
    }

    internal static FileInfo GetEntryPointAssemblyPath(
        this Package package, 
        bool usePublishDir)
    {
        var directory = package.Directory;

        var depsFile = directory.GetFiles("*.deps.json", SearchOption.AllDirectories)
            .FirstOrDefault();

        if (depsFile == null)
        {
            return null;
        }

        var entryPointAssemblyName = DepsFileParser.GetEntryPointAssemblyName(depsFile);

        var path =
            Path.Combine(
                directory.FullName,
                "bin",
                "Debug",
                GetTargetFramework(package));

        if (usePublishDir)
        {
            path = Path.Combine(path, "publish");
        }

        return new FileInfo(Path.Combine(path, entryPointAssemblyName));
    }

    internal static string GetTargetFramework(this Package package)
    {
        var runtimeConfig = package.Directory
                                   .GetFiles("*.runtimeconfig.json", SearchOption.AllDirectories)
                                   .FirstOrDefault();

        return runtimeConfig != null ? RuntimeConfig.GetTargetFramework(runtimeConfig) : "netstandard2.0";
    }
}