// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tools
{
    public static class DirectoryAccessor
    {
        public static bool DirectoryExists(
            this IDirectoryAccessor directoryAccessor,
            string relativePath) =>
            directoryAccessor.DirectoryExists(new RelativeDirectoryPath(relativePath));

        public static bool RootDirectoryExists(
            this IDirectoryAccessor directoryAccessor) =>
            directoryAccessor.DirectoryExists(new RelativeDirectoryPath("."));

        public static void EnsureDirectoryExists(
            this IDirectoryAccessor directoryAccessor,
            string relativePath) =>
            directoryAccessor.EnsureDirectoryExists(new RelativeDirectoryPath(relativePath));

        public static void EnsureRootDirectoryExists(
            this IDirectoryAccessor directoryAccessor) =>
            directoryAccessor.EnsureDirectoryExists(new RelativeDirectoryPath("."));

        public static bool FileExists(
            this IDirectoryAccessor directoryAccessor,
            string relativePath) =>
            directoryAccessor.FileExists(new RelativeFilePath(relativePath));

        public static string ReadAllText(
            this IDirectoryAccessor directoryAccessor,
            string relativePath) =>
            directoryAccessor.ReadAllText(new RelativeFilePath(relativePath));

        public static void WriteAllText(
            this IDirectoryAccessor directoryAccessor,
            string relativePath,
            string text) =>
            directoryAccessor.WriteAllText(
                new RelativeFilePath(relativePath),
                text);

        public static IDirectoryAccessor GetDirectoryAccessorForRelativePath(
            this IDirectoryAccessor directoryAccessor,
            string relativePath) =>
            directoryAccessor.GetDirectoryAccessorForRelativePath(new RelativeDirectoryPath(relativePath));

        public static IDirectoryAccessor GetDirectoryAccessorFor(this IDirectoryAccessor directory, DirectoryInfo directoryInfo)
        {
            var relative = PathUtilities.GetRelativePath(
                directory.GetFullyQualifiedRoot().FullName, 
                directoryInfo.FullName);
            return directory.GetDirectoryAccessorForRelativePath(new RelativeDirectoryPath(relative));
        }

        public static DirectoryInfo GetFullyQualifiedRoot(this IDirectoryAccessor directoryAccessor) =>
            (DirectoryInfo) directoryAccessor.GetFullyQualifiedPath(new RelativeDirectoryPath("."));

        public static FileInfo GetFullyQualifiedFilePath(this IDirectoryAccessor directoryAccessor, string relativeFilePath) => 
            GetFullyQualifiedFilePath(directoryAccessor, new RelativeFilePath(relativeFilePath));

        public static FileInfo GetFullyQualifiedFilePath(this IDirectoryAccessor directoryAccessor, RelativeFilePath relativePath) => 
            (FileInfo) directoryAccessor.GetFullyQualifiedPath(relativePath);

        public static DirectoryInfo GetFullyQualifiedDirectoryPath(this IDirectoryAccessor directoryAccessor, RelativeDirectoryPath relativePath) =>
            (DirectoryInfo)directoryAccessor.GetFullyQualifiedPath(relativePath);
    }
}