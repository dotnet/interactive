// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging;

public class FileTextLoader : TextLoader
{
    private readonly string _absolutePath;

    public FileTextLoader(string absolutePath)
    {
        if (!Path.IsPathRooted(absolutePath))
        {
            throw new ArgumentException("Path must be absolute", nameof(absolutePath));
        }

        _absolutePath = absolutePath;
    }

    public override async Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
    {
        var sourceFile = new FileInfo(_absolutePath);

        var prevLastWriteTime = sourceFile.LastWriteTime;

        var sourceText = SourceText.From(await sourceFile.ReadAsync());

        return TextAndVersion.Create(sourceText, VersionStamp.Create(prevLastWriteTime), _absolutePath);
    }
}