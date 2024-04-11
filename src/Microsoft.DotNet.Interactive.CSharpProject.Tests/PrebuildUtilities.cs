// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Build;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public static class PrebuildUtilities
{
    private static readonly object CreateDirectoryLock = new();

    public static async Task<Prebuild> CreateBuildableCopy(
        this Prebuild fromPrebuild,
        string folderNameStartsWith = null,
        IScheduler buildThrottleScheduler = null,
        DirectoryInfo parentDirectory = null)
    {
        if (fromPrebuild is null)
        {
            throw new ArgumentNullException(nameof(fromPrebuild));
        }

        await fromPrebuild.EnsureReadyAsync();

        folderNameStartsWith ??= fromPrebuild.Name;
        parentDirectory ??= fromPrebuild.Directory.Parent;

        var destination =
            CreateDirectory(folderNameStartsWith,
                            parentDirectory);

        fromPrebuild.Directory.CopyTo(destination, info =>
        {
            switch (info)
            {
                case FileInfo fileInfo:
                    return FileLock.IsLockFile(fileInfo) || fileInfo.Extension.EndsWith("binlog");
                default:
                    return false;
            }
        });

        var copy = new Prebuild(directory: destination, name: destination.Name, enableBuild: true);

        return copy;
    }

    public static DirectoryInfo CreateDirectory(
        string folderNameStartsWith,
        DirectoryInfo parentDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(folderNameStartsWith))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(folderNameStartsWith));
        }

        parentDirectory ??= Prebuild.DefaultPrebuildsDirectory;

        DirectoryInfo created;

        lock (CreateDirectoryLock)
        {
            if (!parentDirectory.Exists)
            {
                parentDirectory.Create();
            }

            var existingFolders = parentDirectory.GetDirectories($"{folderNameStartsWith}.*");

            created = parentDirectory.CreateSubdirectory($"{folderNameStartsWith}.{existingFolders.Length + 1}");
        }

        return created;
    }

    public static async Task<Prebuild> CreateBuildableConsolePrebuildCopy(
        [CallerMemberName] string testName = null,
        IScheduler buildThrottleScheduler = null)
    {
        var prebuild = await Prebuild.GetOrCreateConsolePrebuildAsync(true);
        return await prebuild.CreateBuildableCopy(testName, buildThrottleScheduler);
    }

    public static Prebuild CreateEmptyBuildablePrebuild(
        [CallerMemberName] string testName = null,
        IPrebuildInitializer initializer = null)
    {
        return new Prebuild(
            name: testName,
            directory: CreateDirectory(testName),
            initializer: initializer,
            enableBuild: true);
    }
}