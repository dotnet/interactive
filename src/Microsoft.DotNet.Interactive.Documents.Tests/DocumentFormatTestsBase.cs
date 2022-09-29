// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Microsoft.DotNet.Interactive.Documents.Tests;

public abstract class DocumentFormatTestsBase
{
    protected DocumentFormatTestsBase()
    {
        DefaultKernelInfos = new KernelInfoCollection
        {
            new("csharp", new[] { "cs", "C#", "c#" }),
            new("fsharp", new[] { "fs", "F#", "f#" }),
            new("pwsh", new[] { "powershell" }),
        };
        DefaultKernelInfos.DefaultKernelName = "csharp";
    }

    public KernelInfoCollection DefaultKernelInfos { get; }

    protected static string PathToCurrentSourceFile([CallerFilePath] string path = null)
    {
        return path;
    }
}