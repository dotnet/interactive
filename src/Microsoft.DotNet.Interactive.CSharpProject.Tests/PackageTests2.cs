// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Interactive.CSharpProject.Packaging;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public partial class PackageTests
{
    [Fact]
    public void It_can_have_assets_added_to_it()
    {
        var directoryAccessor = new InMemoryDirectoryAccessor();

        var package = new Package2("the-package", directoryAccessor);
        directoryAccessor.Add(("mainProject.csproj",""));
        var projectAsset = new ProjectAsset(directoryAccessor, "mainProject.csproj");
        package.Add(projectAsset);

        package.Assets.Should().Contain(a => a == projectAsset);
    }

    [Fact]
    public void An_asset_must_be_in_a_subdirectory_of_the_package()
    {
        var directoryAccessor = new InMemoryDirectoryAccessor();
        directoryAccessor.Add(("./2/mainProject.csproj", ""));
        var package = new Package2("1", directoryAccessor.GetDirectoryAccessorForRelativePath("1"));

        var projectAsset = new ProjectAsset(directoryAccessor.GetDirectoryAccessorForRelativePath("2"), "mainProject.csproj");

        package.Invoking(p => p.Add(projectAsset)).Should()
            .Throw<ArgumentException>()
            .And
            .Message
            .Should()
            .StartWith("Asset must be located under package path");
    }

    [Fact]
    public async Task It_discovers_project_assets_in_its_root()
    {
        var package = new Package2(
            "the-package",
            new InMemoryDirectoryAccessor
            {
                ("myapp.csproj", "")
            });

        await package.EnsureLoadedAsync();

        package.Assets.Should().ContainSingle(a => a is ProjectAsset);
        package.Assets.Single().DirectoryAccessor.FileExists("myapp.csproj").Should().BeTrue();
    }

    [Fact]
    public async Task It_discovers_project_assets_in_subfolders()
    {
        var package = new Package2(
            "the-package",
            new InMemoryDirectoryAccessor
            {
                ("./subfolder/myapp.csproj", "")
            });

        await package.EnsureLoadedAsync();

        package.Assets.Should().ContainSingle(a => a is ProjectAsset);
        package.Assets.Single().DirectoryAccessor.FileExists("myapp.csproj").Should().BeTrue();
    }

    [Fact]
    public async Task It_discovers_project_assets_for_previously_installed_packages()
    {
        var directory = ToolPackageDirectoryAccessor();

        var package = new Package2(
            "PACKAGE",
            directory);

        await package.EnsureLoadedAsync(AssetLoaders(directory));

        package.Assets
            .OfType<ProjectAsset>()
            .Single()
            .DirectoryAccessor
            .FileExists("./PACKAGE.csproj")
            .Should()
            .BeTrue();
    }

    private static InMemoryDirectoryAccessor ToolPackageDirectoryAccessor()
    {
        return new InMemoryDirectoryAccessor
        {
            ("PACKAGE".ExecutableName(), null),
            ("./.store/PACKAGE/1.0.0/PACKAGE/1.0.0/tools/netcoreapp2.1/any/project/runner-PACKAGE/MLS.Blazor/runtime/PACKAGE.dll",
                ""),
            ("./.store/PACKAGE/1.0.0/PACKAGE/1.0.0/tools/netcoreapp2.1/any/project/packTarget/PACKAGE.csproj",
                "")
        };
    }

    private static IPackageAssetLoader[] AssetLoaders(InMemoryDirectoryAccessor directory)
    {
        return new IPackageAssetLoader[]
        {
            new ProjectAssetLoader(), 
        };
    }
}