// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public interface IDirectoryAccessor
{
    bool FileExists(RelativeFilePath path);

    bool DirectoryExists(RelativeDirectoryPath path);

    void EnsureDirectoryExists(RelativeDirectoryPath path);

    string ReadAllText(RelativeFilePath path);

    void WriteAllText(RelativeFilePath path, string text);

    IEnumerable<RelativeFilePath> GetAllFilesRecursively();

    IEnumerable<RelativeFilePath> GetAllFiles();

    IEnumerable<RelativeDirectoryPath> GetAllDirectoriesRecursively();

    FileSystemInfo GetFullyQualifiedPath(RelativePath path);

    IDirectoryAccessor GetDirectoryAccessorForRelativePath(RelativeDirectoryPath path);
}