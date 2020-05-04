// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Utility
{
    public class DisposableDirectory : IDisposable
    {
        public DisposableDirectory(DirectoryInfo directory)
        {
            Directory = directory;
        }

        public static DisposableDirectory Create()
        {
            var tempDir = System.IO.Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            return new DisposableDirectory(tempDir);
        }

        public DirectoryInfo Directory { get; }

        public void Dispose()
        {
            Directory.Delete(recursive: true);
        }
    }
}
