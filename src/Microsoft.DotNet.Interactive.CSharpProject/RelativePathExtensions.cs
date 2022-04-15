// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public static class RelativePathExtensions
{
    public static FileInfo Combine(
        this DirectoryInfo directory,
        RelativeFilePath filePath)
    {
        var filePart = filePath.Value;

        if (filePart.StartsWith("./"))
        {
            filePart = filePart.Substring(2);
        }

        return new FileInfo(
            Path.Combine(
                directory.FullName,
                filePart.Replace('/', Path.DirectorySeparatorChar)));
    }

    public static DirectoryInfo Combine(
        this DirectoryInfo directory,
        RelativeDirectoryPath directoryPath)
    {
        return new DirectoryInfo(
            Path.Combine(
                RelativePath.NormalizeDirectory(directory.FullName),
                directoryPath.Value.Replace('/', Path.DirectorySeparatorChar)));
    }

    public static T Match<T>(this RelativePath path, Func<RelativeDirectoryPath, T> directory, Func<RelativeFilePath, T> file)
    {
        switch (path)
        {
            case RelativeDirectoryPath relativeDirectoryPath:
                return directory(relativeDirectoryPath);
            case RelativeFilePath relativeFilePath:
                return file(relativeFilePath);
            default:
                throw new ArgumentOutOfRangeException($"Unexpected type derived from {nameof(RelativePath)}: {path.GetType().Name}");
        }
    }
}