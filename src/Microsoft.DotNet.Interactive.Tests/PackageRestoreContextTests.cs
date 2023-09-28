// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.PackageManagement;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests;

public class PackageRestoreContextTests : LanguageKernelTestBase
{
    public PackageRestoreContextTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Returns_new_references_if_they_are_added()
    {
        using var restoreContext = new PackageRestoreContext(false);
        var added = restoreContext.GetOrAddPackageReference("FluentAssertions", "5.7.0");
        added.Should().NotBeNull();

        var result = await restoreContext.RestoreAsync();

        result.Errors.Should().BeEmpty();
        var assemblyPaths = result.ResolvedReferences.SelectMany(r => r.AssemblyPaths).ToArray();

        assemblyPaths.Should().Contain(r => r.EndsWith("FluentAssertions.dll"));
        assemblyPaths.Should().Contain(r => r.EndsWith("System.Configuration.ConfigurationManager.dll"));

        restoreContext
            .ResolvedPackageReferences
            .Should()
            .Contain(r => r.PackageName.Equals("FluentAssertions", StringComparison.OrdinalIgnoreCase) &&
                          r.PackageVersion == "5.7.0");
    }

    [Fact]
    public async Task Returns_references_when_package_version_is_not_specified()
    {
        using var restoreContext = new PackageRestoreContext(false);
        var added = restoreContext.GetOrAddPackageReference("NewtonSoft.Json");
        added.Should().NotBeNull();

        var result = await restoreContext.RestoreAsync();

        result.Succeeded.Should().BeTrue();
            
        var assemblyPaths = result.ResolvedReferences.SelectMany(r => r.AssemblyPaths);
        assemblyPaths.Should().Contain(r => r.EndsWith("NewtonSoft.Json.dll", StringComparison.InvariantCultureIgnoreCase));

        restoreContext
            .ResolvedPackageReferences
            .Should()
            .Contain(r => r.PackageName.Equals("NewtonSoft.Json", StringComparison.OrdinalIgnoreCase) &&
                          !string.IsNullOrWhiteSpace(r.PackageVersion));
    }

    [Fact]
    public async Task Returns_failure_if_package_installation_fails()
    {
        using var restoreContext = new PackageRestoreContext(false);
        var added = restoreContext.GetOrAddPackageReference("not-a-real-package-definitely-not", "5.7.0");
        added.Should().NotBeNull();

        var result = await restoreContext.RestoreAsync();
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Returns_failure_if_adding_package_twice_at_different_versions()
    {
        using var restoreContext = new PackageRestoreContext(false);
        var added = restoreContext.GetOrAddPackageReference("another-not-a-real-package-definitely-not", "5.7.0");
        added.Should().NotBeNull();

        var readded = restoreContext.GetOrAddPackageReference("another-not-a-real-package-definitely-not", "5.7.1");
        readded.Should().BeNull();

        var result = await restoreContext.RestoreAsync();
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task A_failing_package_restore_does_not_cause_future_resolves_to_fail()

    {
        using var restoreContext = new PackageRestoreContext(false);
        var added = restoreContext.GetOrAddPackageReference("FluentAssertions", "5.7.0");
        added.Should().NotBeNull();

        var firstResult = await restoreContext.RestoreAsync();

        firstResult.ResolvedReferences
            .Should()
            .Contain(r => r.PackageName == "FluentAssertions");

        var readded = restoreContext.GetOrAddPackageReference("FluentAssertions", "5.7.0");
        readded.Should().NotBeNull();

        var secondResult = await restoreContext.RestoreAsync();

        secondResult.ResolvedReferences
            .Should()
            .NotContain(r => r.PackageName == "FluentAssertions");
    }

    [Fact]
    public async Task Invalid_package_restores_are_not_remembered()
    {
        using var restoreContext = new PackageRestoreContext(false);

        // This package does not exist
        restoreContext.GetOrAddPackageReference("NonExistentNugetPackage", "99.99.99-NoReallyIDontExist");
        var setupResult = await restoreContext.RestoreAsync();
        setupResult.Succeeded.Should().BeFalse();

        // Even though the previous restore failed, this one should succeed
        restoreContext.GetOrAddPackageReference("FluentAssertions", "5.7.0");
        var result = await restoreContext.RestoreAsync();

        result.ResolvedReferences
            .Should()
            .Contain(r => r.PackageName == "FluentAssertions");
    }


    [Fact]
    public async Task Can_get_path_to_nuget_packaged_assembly()
    {
        using var restoreContext = new PackageRestoreContext(false);
        restoreContext.GetOrAddPackageReference("fluentAssertions", "5.7.0");

        await restoreContext.RestoreAsync();

        var packageReference = restoreContext.GetResolvedPackageReference("fluentassertions");

        var path = packageReference.AssemblyPaths.Single();

        // path is a string similar to:
        /// c:/users/someuser/.nuget/packages/fluentassertions/5.7.0/netcoreapp2.0/fluentassertions.dll
        var dll = new FileInfo(path);
        var tfm = dll.Directory.Name;
        var reflib = dll.Directory.Parent.Name;
        var version = dll.Directory.Parent.Parent.Name;
        var packageName = dll.Directory.Parent.Parent.Parent.Name;

        dll.Name.ToLower().Should().Be("fluentassertions.dll");
        tfm.ToLower().Should().Be("netcoreapp2.0");
        reflib.ToLower().Should().Be("lib");
        version.ToLower().Should().Be("5.7.0");
        packageName.ToLower().Should().Be("fluentassertions");
        dll.Exists.Should().BeTrue();
    }

    [Fact]
    public async Task Can_get_path_to_nuget_package_root()
    {
        using var restoreContext = new PackageRestoreContext(false);
        restoreContext.GetOrAddPackageReference("fluentAssertions", "5.7.0");

        await restoreContext.RestoreAsync();

        var packageReference = restoreContext.GetResolvedPackageReference("fluentassertions");

        var path = packageReference.PackageRoot;

        var directory = new DirectoryInfo(path);
        var version = directory.Name;
        var packageName = directory.Parent.Name;
        version.ToLower().Should().Be("5.7.0");
        packageName.ToLower().Should().Be("fluentassertions");
        directory.Exists.Should().BeTrue();
    }

    [Fact]
    public async Task Can_get_path_to_nuget_package_when_multiple_packages_are_added()
    {
        using var restoreContext = new PackageRestoreContext(false);
        restoreContext.GetOrAddPackageReference("fluentAssertions", "5.7.0");
        restoreContext.GetOrAddPackageReference("htmlagilitypack", "1.11.12");

        await restoreContext.RestoreAsync();

        var packageReference = restoreContext.GetResolvedPackageReference("htmlagilitypack");

        var path = packageReference.AssemblyPaths.Single();

        path.ToLower()
            .Should()
            .EndWith("htmlagilitypack" + Path.DirectorySeparatorChar +
                     "1.11.12" + Path.DirectorySeparatorChar +
                     "lib" + Path.DirectorySeparatorChar +
                     "netstandard2.0" + Path.DirectorySeparatorChar +
                     "htmlagilitypack.dll");
        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public async Task Fail_if_restore_source_has_an_invalid_uri()
    {
        using var restoreContext = new PackageRestoreContext(false);
        restoreContext.TryAddRestoreSource("https://completelyFakerestore Source");
        var result = await restoreContext.RestoreAsync();
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Can_add_to_list_of_added_sources()
    {
        using var restoreContext = new PackageRestoreContext(false);

        restoreContext.TryAddRestoreSource("https://completelyFakerestoreSource");
        await restoreContext.RestoreAsync();
        var restoreSources = restoreContext.RestoreSources;
        restoreSources.Should().ContainSingle("https://completelyFakerestoreSource");
    }

    [Fact]
    public async Task Can_add_same_source_to_list_of_added_sources_without_error()
    {
        using var restoreContext = new PackageRestoreContext(false);

        var savedRestoreSources = restoreContext.RestoreSources.ToArray();
        restoreContext.TryAddRestoreSource("https://completelyFakerestoreSource");
        restoreContext.TryAddRestoreSource("https://completelyFakerestoreSource");
        await restoreContext.RestoreAsync();
        var restoreSources = restoreContext.RestoreSources.Where(p => !savedRestoreSources.Contains(p));
        restoreSources.Should()
            .ContainSingle("https://completelyFakerestoreSource");
    }

    [Fact]
    public async Task Allows_duplicate_package_specifications()
    {
        using var restoreContext = new PackageRestoreContext(false);
        restoreContext.GetOrAddPackageReference("NodaTime", "3.1.9");
        restoreContext.GetOrAddPackageReference("NodaTime", "3.1.9");

        await restoreContext.RestoreAsync();

        var resolvedPackageReferences = restoreContext.ResolvedPackageReferences;
        resolvedPackageReferences.Should()
            .ContainSingle(r => r.PackageName == "NodaTime" && r.PackageVersion == "3.1.9");
    }

    [Fact]
    // Question:   should it not throw, or is ignore sufficient
    public async Task Ignores_subsequent_package_specifications_with_different_higher_version()
    {
        using var restoreContext = new PackageRestoreContext(false);
        restoreContext.GetOrAddPackageReference("NodaTime", "3.1.0");
        restoreContext.GetOrAddPackageReference("NodaTime", "3.1.9");

        await restoreContext.RestoreAsync();

        var resolvedPackageReferences = restoreContext.ResolvedPackageReferences;
        resolvedPackageReferences.Should()
            .ContainSingle(r => r.PackageName == "NodaTime" && r.PackageVersion == "3.1.0");
    }

    [Fact]
    public async Task Disallows_package_specifications_with_different_lower_version()
    {
        using var restoreContext = new PackageRestoreContext(false);
        restoreContext.GetOrAddPackageReference("NodaTime", "3.1.9");
        restoreContext.GetOrAddPackageReference("NodaTime", "3.1.0");
        await restoreContext.RestoreAsync();

        var resolvedPackageReferences = restoreContext.ResolvedPackageReferences;
        resolvedPackageReferences.Should()
            .ContainSingle(r => r.PackageName == "NodaTime" && r.PackageVersion == "3.1.9");
    }

    [Fact]
    public async Task Disallows_package_specifications_with_different_lower_unspecified_version_first()
    {
        using var restoreContext = new PackageRestoreContext(false);
        restoreContext.GetOrAddPackageReference("NodaTime", "*");
        restoreContext.GetOrAddPackageReference("NodaTime", "3.1.0");

        await restoreContext.RestoreAsync();

        var resolvedPackageReferences = restoreContext.ResolvedPackageReferences;
        resolvedPackageReferences.Should()
            .ContainSingle(r => r.PackageName == "NodaTime" && r.PackageVersion != "3.1.0");
    }

    [Fact]
    public async Task Disallows_package_specifications_with_different_lower_unspecified_version_last()
    {
        using var restoreContext = new PackageRestoreContext(false);
        restoreContext.GetOrAddPackageReference("NodaTime", "3.1.0");
        restoreContext.GetOrAddPackageReference("NodaTime", "*");

        await restoreContext.RestoreAsync();

        var resolvedPackageReferences = restoreContext.ResolvedPackageReferences;
        resolvedPackageReferences.Should()
            .ContainSingle(r => r.PackageName == "NodaTime" && r.PackageVersion == "3.1.0");
    }
}