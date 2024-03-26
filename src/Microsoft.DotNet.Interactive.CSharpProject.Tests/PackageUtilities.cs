﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Build;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public static class PackageUtilities
{
    private static readonly object CreateDirectoryLock = new();

    public static async Task<Package> CreateBuildableCopy(
        this Package fromPackage,
        string folderNameStartsWith = null,
        IScheduler buildThrottleScheduler = null,
        DirectoryInfo parentDirectory = null)
    {
        if (fromPackage is null)
        {
            throw new ArgumentNullException(nameof(fromPackage));
        }

        await fromPackage.EnsureReadyAsync();

        folderNameStartsWith ??= fromPackage.Name;
        parentDirectory ??= fromPackage.Directory.Parent;

        var destination =
            CreateDirectory(folderNameStartsWith,
                            parentDirectory);

        fromPackage.Directory.CopyTo(destination, info =>
        {
            switch (info)
            {
                case FileInfo fileInfo:
                    return FileLock.IsLockFile(fileInfo) || fileInfo.Extension.EndsWith("binlog");
                default:
                    return false;
            }
        });

        var copy = new Package(directory: destination, name: destination.Name, enableBuild: true);

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

        parentDirectory ??= Package.DefaultPackagesDirectory;

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

    public static async Task<Package> CreateBuildableConsolePackageCopy(
        [CallerMemberName] string testName = null,
        IScheduler buildThrottleScheduler = null)
    {
        var consolePackage = await Package.GetOrCreateConsolePackageAsync(true);
        return await consolePackage.CreateBuildableCopy(testName, buildThrottleScheduler);
    }

    public static Package CreateEmptyBuildablePackage(
        [CallerMemberName] string testName = null,
        IPackageInitializer initializer = null)
    {
        return new Package(
            name: testName,
            directory: CreateDirectory(testName),
            initializer: initializer,
            enableBuild: true);
    }
}