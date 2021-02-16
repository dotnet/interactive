// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.Tests.Utility
{
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
}
