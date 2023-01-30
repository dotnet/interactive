// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.CSharpProject.Packaging;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;

internal static class CompilationUtility
{
    internal static bool CanBeUsedToGenerateCompilation(this CodeAnalysis.Workspace workspace)
    {
        return workspace?.CurrentSolution?.Projects?.Count() > 0;
    }

    public static IEnumerable<FileInfo> FindBinLogs(this IHaveADirectory package) =>
        package.Directory
            .GetFiles("*.binlog")
            .Where(f => f.FullName.EndsWith(PackageBase.FullBuildBinlogFileName) ||
                        f.FullName.EndsWith(Package.DesignTimeBuildBinlogFileName));

    public static async Task WaitForFileAvailable(
        this FileInfo file)
    {
        if (file == null)
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

    public static async Task DoWhenFileAvailable(
        this FileInfo file, Action action)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        const int waitAmount = 100;
        var attemptCount = 1;
        while (file.Exists && attemptCount <= 10)
        {
            try
            {
                action();
                break;
            }
            catch(IOException){
                await Task.Delay(waitAmount * attemptCount);
                attemptCount++;
            }
        }
    }

    public static FileInfo GetProjectFile(this IHaveADirectory packageBase) =>
        packageBase.Directory.GetFiles("*.csproj").FirstOrDefault();

    public static void CleanObjFolder(this IHaveADirectory packageBase)
    {
        var targets = packageBase.Directory.GetDirectories("obj");
        foreach (var target in targets)
        {
            target.Delete(true);
        }
    }

    public static FileInfo FindLatestBinLog(this IHaveADirectory package) =>
        package.FindBinLogs().OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();

    internal static FileInfo GetEntryPointAssemblyPath(
        this IHaveADirectory hasDirectory, 
        bool usePublishDir)
    {
        var directory = hasDirectory.Directory;

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
                GetTargetFramework(hasDirectory));

        if (usePublishDir)
        {
            path = Path.Combine(path, "publish");
        }

        return new FileInfo(Path.Combine(path, entryPointAssemblyName));
    }

    internal static string GetTargetFramework(this IHaveADirectory ihad)
    {
        var runtimeConfig = ihad.Directory
            .GetFiles("*.runtimeconfig.json", SearchOption.AllDirectories)
            .FirstOrDefault();

        return runtimeConfig != null ? RuntimeConfig.GetTargetFramework(runtimeConfig) : "netstandard2.0";
    }
}