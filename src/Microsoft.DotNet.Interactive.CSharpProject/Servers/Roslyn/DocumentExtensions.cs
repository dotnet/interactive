// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;

internal static class DocumentExtensions
{
    public static bool IsMatch(this Document doc, ProjectFileContent fileContent) => 
        doc.IsMatch(fileContent.Name);

    public static bool IsMatch(this Document d, SourceFile source) => 
        d.IsMatch(source.Name);

    public static bool IsMatch(this Document d, string sourceName) =>
        string.Compare(d.Name, sourceName, StringComparison.OrdinalIgnoreCase) == 0 ||
        string.Compare(d.FilePath, sourceName, StringComparison.OrdinalIgnoreCase) == 0 ||
        (!string.IsNullOrWhiteSpace(sourceName) && (string.Compare(new RelativeFilePath(sourceName).Value, new RelativeFilePath(d.Name).Value, StringComparison.OrdinalIgnoreCase) == 0));
}