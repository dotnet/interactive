// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging;

public class FileLock
{
    private const string LockFileName = ".trydotnet-lock";

    public static async Task<IDisposable> TryCreateAsync(FileInfo lockFile)
    {
        if (lockFile == null)
        {
            throw new ArgumentNullException(nameof(lockFile));
        }

        const int waitAmount = 100;
        var attemptCount = 1;
        do
        {
            await Task.Delay(waitAmount * attemptCount);
            attemptCount++;

            try
            {
                return File.Create(lockFile.FullName, 1, FileOptions.DeleteOnClose);
            }
            catch (IOException)
            {

            }
        } while (attemptCount <= 100);

        throw new IOException($"Cannot acquire file lock {lockFile.FullName}");
    }

    public static Task<IDisposable> TryCreateAsync(IDirectoryAccessor directoryAccessor)
    {
        if (directoryAccessor == null)
        {
            throw new ArgumentNullException(nameof(directoryAccessor));
        }
        return TryCreateAsync(directoryAccessor.GetFullyQualifiedFilePath(LockFileName));
    }

    public static Task<IDisposable> TryCreateAsync(DirectoryInfo directory)
    {
        if (directory == null)
        {
            throw new ArgumentNullException(nameof(directory));
        }
        var lockFile = new FileInfo(Path.Combine(directory.FullName, LockFileName));
        return TryCreateAsync(lockFile);
    }

    public static bool IsLockFile(FileInfo fileInfo)
    {
        if (fileInfo == null)
        {
            throw new ArgumentNullException(nameof(fileInfo));
        }
        return fileInfo.Name == LockFileName;
    }
}