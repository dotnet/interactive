// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class RelativeFilePath :
    RelativePath,
    IEquatable<RelativeFilePath>
{
    public RelativeFilePath(string value) : base(value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("File path cannot be null or consist entirely of whitespace", nameof(value));
        }

        var (directoryPath, fileName) = GetFileAndDirectoryNames(value);

        FileName = fileName;

        ThrowIfContainsDisallowedFilePathChars(FileName);

        Directory = new RelativeDirectoryPath(directoryPath);

        Value = Directory.Value + FileName;
    }

    private static (string directoryPath, string fileName) GetFileAndDirectoryNames(string filePath)
    {
        var lastDirectorySeparatorPos = filePath.LastIndexOfAny(new[] { '\\', '/' });

        if (lastDirectorySeparatorPos == -1)
        {
            return ("./", filePath);
        }

        var fileName = filePath.Substring(lastDirectorySeparatorPos + 1);

        var directoryPath = filePath.Substring(0, lastDirectorySeparatorPos);

        directoryPath = NormalizeDirectory(directoryPath);

        return (directoryPath, fileName);
    }

    public string FileName { get; }

    public RelativeDirectoryPath Directory { get; }

    public string Extension =>
        Path.GetExtension(Value);

    public static bool TryParse(string path, out RelativeFilePath relativeFilePath)
    {
        relativeFilePath = null;
        try
        {
            relativeFilePath = new RelativeFilePath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool Equals(RelativeFilePath other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(FileName, other.FileName) &&
               Equals(Directory, other.Directory);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((RelativeFilePath) obj);
    }

    public override int GetHashCode() => Value.GetHashCode();
}