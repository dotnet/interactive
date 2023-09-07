// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class FileSystemDirectoryAccessor : IDirectoryAccessor
{
    private readonly DirectoryInfo _rootDirectory;

    public FileSystemDirectoryAccessor(DirectoryInfo rootDir)
    {
        _rootDirectory = rootDir ?? throw new ArgumentNullException(nameof(rootDir));
    }

    public bool DirectoryExists(RelativeDirectoryPath path)
    {
        return GetFullyQualifiedPath(path).Exists;
    }

    public bool FileExists(RelativeFilePath filePath)
    {
        return GetFullyQualifiedPath(filePath).Exists;
    }

    public void EnsureDirectoryExists(RelativeDirectoryPath path)
    {
        var fullyQualifiedPath = GetFullyQualifiedPath(path);

        if (!Directory.Exists(fullyQualifiedPath.FullName))
        {
            Directory.CreateDirectory(fullyQualifiedPath.FullName);
        }
    }

    public string ReadAllText(RelativeFilePath filePath)
    {
        return File.ReadAllText(GetFullyQualifiedPath(filePath).FullName);
    }

    public void WriteAllText(RelativeFilePath path, string text)
    {
        File.WriteAllText(GetFullyQualifiedPath(path).FullName, text);
    }

    public FileSystemInfo GetFullyQualifiedPath(RelativePath path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        return path.Match<FileSystemInfo>(
            directory => new DirectoryInfo(_rootDirectory.Combine(directory).FullName),
            file => new FileInfo(_rootDirectory.Combine(file).FullName)
        );
    }

    public IDirectoryAccessor GetDirectoryAccessorForRelativePath(RelativeDirectoryPath relativePath)
    {
        var absolutePath = _rootDirectory.Combine(relativePath).FullName;
        return new FileSystemDirectoryAccessor(new DirectoryInfo(absolutePath));
    }

    public IEnumerable<RelativeDirectoryPath> GetAllDirectoriesRecursively()
    {
        var directories = _rootDirectory.GetDirectories("*", SearchOption.AllDirectories);

        return directories.Select(f =>
            new RelativeDirectoryPath(PathUtilities.GetRelativePath(_rootDirectory.FullName, f.FullName)));
    }

    public IEnumerable<RelativeFilePath> GetAllFilesRecursively()
    {
        var files = _rootDirectory.GetFiles("*", SearchOption.AllDirectories);

        return files.Select(f =>
            new RelativeFilePath(PathUtilities.GetRelativePath(_rootDirectory.FullName, f.FullName)));
    }

    public IEnumerable<RelativeFilePath> GetAllFiles()
    {
        var files = _rootDirectory.GetFiles("*", SearchOption.TopDirectoryOnly);

        return files.Select(f =>
            new RelativeFilePath(PathUtilities.GetRelativePath(_rootDirectory.FullName, f.FullName)));
    }

    public override string ToString() => _rootDirectory.FullName;
}