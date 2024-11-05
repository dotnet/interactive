// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;

public static class WorkspaceUtilities
{
    public static readonly ImmutableArray<string> DefaultUsingDirectives = new[]
    {
        "System",
        "System.Linq",
        "System.Collections.Generic",
        "System.Globalization",
        "System.Threading.Tasks"
    }.ToImmutableArray();

    public static IEnumerable<MetadataReference> GetMetadataReferences(this IEnumerable<string> filePaths)
    {
        foreach (var filePath in filePaths)
        {
            var expectedXmlFile =
                filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                    ? ReplaceCaseInsensitive(filePath, ".dll", ".xml")
                    : Path.Combine(Paths.InstallDirectory,
                        "completion",
                        "references",
                        $"{Path.GetFileName(filePath)}.xml");

            yield return MetadataReference.CreateFromFile(
                filePath,
                documentation: XmlDocumentationProvider.CreateFromFile(expectedXmlFile));
        }
    }

    private static string ReplaceCaseInsensitive(string str, string toReplace, string replacement)
    {
        var index = str.IndexOf(toReplace, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            str = str.Remove(index, toReplace.Length);
            str = str.Insert(index, replacement);
        }

        return str;
    }
}