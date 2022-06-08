// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Mermaid;

public static class MermaidExtensions
{

    public static T UseMermaid<T>(this T kernel, Uri? libraryUri = null, string? libraryVersion = null, string? cacheBuster = null) where T : Kernel
    {
        MermaidMarkdownFormatter.LibraryUri = libraryUri;
        MermaidMarkdownFormatter.LibraryVersion = libraryVersion;
        MermaidMarkdownFormatter.CacheBuster = cacheBuster;
        return kernel;
    }

  
}