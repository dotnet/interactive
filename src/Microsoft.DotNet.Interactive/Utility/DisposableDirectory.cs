// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

#nullable enable

namespace Microsoft.DotNet.Interactive.Utility;

public class DisposableDirectory : IDisposable
{
    private DisposableDirectory(DirectoryInfo directory)
    {
        Directory = directory;
    }

    public static DisposableDirectory Create(string? directoryName = null)
    {
        var tempDir = System.IO.Directory.CreateDirectory(
            Path.Combine(
                Path.GetTempPath(),
                directoryName ?? Path.GetRandomFileName()));
        return new DisposableDirectory(tempDir);
    }

    public DirectoryInfo Directory { get; }

    public void Dispose()
    {
        try
        {
            Directory.Delete(recursive: true);
        }
        catch
        {
        }
    }
}