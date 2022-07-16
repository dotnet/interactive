// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Documents.Tests;

public abstract class DocumentFormatTestsBase
{
    public KernelNameCollection KernelNames { get; }

    protected DocumentFormatTestsBase()
    {
        KernelNames = new KernelNameCollection
        {
            new("csharp", new[] { "cs", "C#", "c#" }),
            new("fsharp", new[] { "fs", "F#", "f#" }),
            new("pwsh", new[] { "powershell" }),
        };
        KernelNames.DefaultKernelName = "csharp";
    }
}