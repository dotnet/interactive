// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.CSharpProject.Packaging;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public class Default
{
    // FIX: (Default) delete?
    public static IPackageFinder PackageFinder => Packaging.PackageFinder.Create(CSharpProjectKernel.CreateConsolePackageAsync);
}