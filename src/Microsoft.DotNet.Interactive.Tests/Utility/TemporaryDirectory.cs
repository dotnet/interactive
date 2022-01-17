// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

public class TemporaryDirectory : IDisposable
{
    public DirectoryInfo Directory { get; }

    public TemporaryDirectory(params (string relativePath, string content)[] contents)
    {
        var tempPath = Path.GetTempPath();
        var dirName = Guid.NewGuid().ToString();
        var fullPath = Path.Combine(tempPath, dirName);
        var parentDirectory = new DirectoryInfo(fullPath);
        Directory = DirectoryUtility.CreateDirectory(parentDirectory: parentDirectory);
        DirectoryUtility.Populate(Directory, contents);
    }

    public static TemporaryDirectory CreateFromDeepCopy(DirectoryInfo source)
    {
        var tmp = new TemporaryDirectory();
        
        CopyAll(source, tmp.Directory);

        static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            target.Create();

            // Copy each file into the new directory.
            foreach (var fi in source.GetFiles())
            {
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each sub-directory using recursion.
            foreach (var diSourceSubDir in source.GetDirectories())
            {
                var nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        return tmp;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(true);
        }
        catch
        {
        }
    }
}