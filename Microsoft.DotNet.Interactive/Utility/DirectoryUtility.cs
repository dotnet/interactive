// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.DotNet.Interactive.Utility
{
    public static class DirectoryUtility
    {
        private static readonly object _lock = new object();

        private static readonly DirectoryInfo _defaultDirectory = new DirectoryInfo(
            Path.Combine(
                Paths.UserProfile,
                ".net-interactive-tests"));

        public static DirectoryInfo CreateDirectory(
            [CallerMemberName] string folderNameStartsWith = null,
            DirectoryInfo parentDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(folderNameStartsWith))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(folderNameStartsWith));
            }

            parentDirectory = parentDirectory ?? _defaultDirectory;

            DirectoryInfo created;

            lock (_lock)
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

        public static void DeleteFileSystemObject(this string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                throw new ArgumentException($"Couldn't find a file or directory called {path}");
            }
        }

        public static void Populate(
            this DirectoryInfo directory,
            params (string relativePath, string content)[] contents)
        {
            EnsureExists(directory);

            foreach (var t in contents)
            {
                File.WriteAllText(
                    Path.Combine(directory.FullName, t.relativePath),
                    t.content);
            }
        }

        public static DirectoryInfo EnsureExists(this DirectoryInfo directory)
        {
            if (directory == null)
            {
                throw new ArgumentNullException(nameof(directory));
            }

            if (!directory.Exists)
            {
                directory.Create();
            }

            return directory;
        }
    }
}