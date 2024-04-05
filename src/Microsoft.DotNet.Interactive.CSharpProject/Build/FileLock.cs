// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Pocket;

namespace Microsoft.DotNet.Interactive.CSharpProject.Build;

[DebuggerStepThrough]
public class FileLock
{
    private const string LockFileName = ".trydotnet-lock";
    
    public static Task<IDisposable> TryCreateAsync(DirectoryInfo directory)
    {
        if (directory is null)
        {
            throw new ArgumentNullException(nameof(directory));
        }
        var lockFile = new FileInfo(Path.Combine(directory.FullName, LockFileName));
        return TryCreateAsync(lockFile);
    }

    public static bool IsLockFile(FileInfo fileInfo)
    {
        if (fileInfo is null)
        {
            throw new ArgumentNullException(nameof(fileInfo));
        }

        return fileInfo.Name == LockFileName;
    }

    private static async Task<IDisposable> TryCreateAsync(FileInfo lockFile)
    {
        if (lockFile is null)
        {
            throw new ArgumentNullException(nameof(lockFile));
        }

        var attemptCount = 1;

        while (attemptCount <= 100)
        {
            try
            {
                return File.Create(lockFile.FullName, 1, FileOptions.DeleteOnClose);
            }
            catch (IOException)
            {
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
            attemptCount++;

            if (attemptCount % 10 == 0)
            {
                Logger.Log.Info($"Waiting on {nameof(FileLock)} for {attemptCount / 10} seconds");
            }
        }

        throw new IOException($"Cannot acquire file lock {lockFile.FullName} after {attemptCount} attempts.");
    }
}