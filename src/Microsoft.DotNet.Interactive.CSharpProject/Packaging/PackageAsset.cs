// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging;

public abstract class PackageAsset
{
    protected PackageAsset(IDirectoryAccessor directoryAccessor)
    {
        DirectoryAccessor = directoryAccessor ?? throw new ArgumentNullException(nameof(directoryAccessor));
    }

    public IDirectoryAccessor DirectoryAccessor { get; }
}

public class WebAssemblyAsset : PackageAsset
{
    public WebAssemblyAsset(IDirectoryAccessor directoryAccessor) : base(directoryAccessor)
    {
    }
}

public class ContentAsset : PackageAsset
{
    public ContentAsset(IDirectoryAccessor directoryAccessor) : base(directoryAccessor)
    {
    }
}