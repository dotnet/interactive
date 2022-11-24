// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public static class DirectoryInfoExtensions
{
    public static void CopyTo(
        this DirectoryInfo source,
        DirectoryInfo destination,
        Func<FileSystemInfo,bool> skipWhen = null)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (!source.Exists)
        {
            throw new DirectoryNotFoundException(source.FullName);
        }

        if (!destination.Exists)
        {
            destination.Create();
        }

        foreach (var file in source.GetFiles())
        {
            if (skipWhen?.Invoke(file) == true)
            {
                continue;
            }

            file.CopyTo(
                Path.Combine(
                    destination.FullName, file.Name), false);
        }

        foreach (var subdirectory in source.GetDirectories())
        {
            if (skipWhen?.Invoke(subdirectory) == true)
            {
                continue;
            }

            subdirectory.CopyTo(
                new DirectoryInfo(
                    Path.Combine(
                        destination.FullName, subdirectory.Name)));
        }
    }

    public static DirectoryInfo Subdirectory(this DirectoryInfo directoryInfo, string path)
    {
        return new DirectoryInfo(Path.Combine(directoryInfo.FullName, path));
    }

    public static FileInfo File(this DirectoryInfo directoryInfo, string name)
    {
        return new FileInfo(Path.Combine(directoryInfo.FullName, name));
    }

    public static DirectoryInfo NormalizeEnding(this DirectoryInfo directoryInfo)
    {
        if (!directoryInfo.FullName.EndsWith(Path.DirectorySeparatorChar.ToString()))
        {
            return new DirectoryInfo(Path.Combine(directoryInfo.FullName, Path.DirectorySeparatorChar.ToString()));
        }

        return directoryInfo;
    }
}