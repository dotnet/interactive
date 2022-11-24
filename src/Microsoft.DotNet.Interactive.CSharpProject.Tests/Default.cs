// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Packaging;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public class Default
{
    public static IPackageFinder PackageRegistry => PackageFinder.Create(ConsoleWorkspaceAsync);

    public static async Task<Package> ConsoleWorkspaceAsync()
    {
        var packageBuilder = new PackageBuilder("console");
        packageBuilder.CreateUsingDotnet("console");
        packageBuilder.TrySetLanguageVersion("8.0");
        packageBuilder.AddPackageReference("Newtonsoft.Json", "13.0.1");
        var package = packageBuilder.GetPackage() as Package;
        await package.CreateWorkspaceForRunAsync();
        return package;
    }
}