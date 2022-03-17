// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tools
{
    public abstract class RelativePath
    {
        private string _value;

        protected RelativePath(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            ThrowIfPathIsRooted(value);
        }

        private void ThrowIfPathIsRooted(string path)
        {
            if (IsPathRootedRegardlessOfOS(path))
            {
                throw new ArgumentException($"Path cannot be absolute: {path}");
            }
        }

        private static bool IsPathRootedRegardlessOfOS(string path)
        {
            return Path.IsPathRooted(path) ||
                   path.StartsWith(@"/") ||
                   path.StartsWith(@"\\") ||
                   path.Substring(1).StartsWith(@":\");
        }

        public string Value
        {
            get => _value;
            protected set => _value = value ?? throw new ArgumentNullException(nameof(value)) ;
        }

        public override string ToString() => Value;

        private static readonly HashSet<char> DisallowedPathChars = new HashSet<char>(
            new char[]
            {
                '|',
                '\0',
                '\u0001',
                '\u0002',
                '\u0003',
                '\u0004',
                '\u0005',
                '\u0006',
                '\a',
                '\b',
                '\t',
                '\n',
                '\v',
                '\f',
                '\r',
                '\u000e',
                '\u000f',
                '\u0010',
                '\u0011',
                '\u0012',
                '\u0013',
                '\u0014',
                '\u0015',
                '\u0016',
                '\u0017',
                '\u0018',
                '\u0019',
                '\u001a',
                '\u001b',
                '\u001c',
                '\u001d',
                '\u001e',
                '\u001f'
            });

        private static readonly HashSet<char> DisallowedFileNameChars = new HashSet<char>(
            new char[]
            {
                '"',
                '<',
                '>',
                '|',
                '\0',
                '\u0001',
                '\u0002',
                '\u0003',
                '\u0004',
                '\u0005',
                '\u0006',
                '\a',
                '\b',
                '\t',
                '\n',
                '\v',
                '\f',
                '\r',
                '\u000e',
                '\u000f',
                '\u0010',
                '\u0011',
                '\u0012',
                '\u0013',
                '\u0014',
                '\u0015',
                '\u0016',
                '\u0016',
                '\u0017',
                '\u0018',
                '\u0019',
                '\u001a',
                '\u001b',
                '\u001c',
                '\u001d',
                '\u001e',
                '\u001f',
                ':',
                '*',
                '?',
                '\\'
            });

        public static string NormalizeDirectory(string directoryPath)
        {
            directoryPath = directoryPath.Replace('\\', '/');

            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                directoryPath = "./";
            }
            else
            {
                if (!IsPathRootedRegardlessOfOS(directoryPath) &&
                    !directoryPath.StartsWith(".") && 
                    !directoryPath.StartsWith("..") && 
                    !directoryPath.StartsWith("/"))
                {
                    directoryPath = $"./{directoryPath}";
                }

                directoryPath = directoryPath.TrimEnd('\\', '/') + '/';
            }

            ThrowIfContainsDisallowedDirectoryPathChars(directoryPath);

            return directoryPath;
        }

        protected static void ThrowIfContainsDisallowedDirectoryPathChars(string path)
        {
            for (var index = 0; index < path.Length; index++)
            {
                var ch = path[index];
                if (DisallowedPathChars.Contains(ch))
                {
                    throw new ArgumentException($"The character {ch} is not allowed in the path");
                }
            }
        }

        protected static void ThrowIfContainsDisallowedFilePathChars(string filename)
        {
            for (var index = 0; index < filename.Length; index++)
            {
                var ch = filename[index];
                if (DisallowedFileNameChars.Contains(ch))
                {
                    throw new ArgumentException($"The character {ch} is not allowed in the filename");
                }
            }
        }

        // public static bool operator ==(RelativePath left, RelativePath right)
        // {
        //     return Equals(left, right);
        // }
        //
        // public static bool operator !=(RelativePath left, RelativePath right)
        // {
        //     return !Equals(left, right);
        // }
    }
}