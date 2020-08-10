// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class PackageRestoreContextTests : LanguageKernelTestBase
    {
        public PackageRestoreContextTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task Returns_new_references_if_they_are_added()
        {
            using var restoreContext = new PackageRestoreContext();
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
            using var restoreContext = new PackageRestoreContext();
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
            using var restoreContext = new PackageRestoreContext();
            var added = restoreContext.GetOrAddPackageReference("not-a-real-package-definitely-not", "5.7.0");
            added.Should().NotBeNull();

            var result = await restoreContext.RestoreAsync();
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Returns_failure_if_adding_package_twice_at_different_versions()
        {
            using var restoreContext = new PackageRestoreContext();
            var added = restoreContext.GetOrAddPackageReference("another-not-a-real-package-definitely-not", "5.7.0");
            added.Should().NotBeNull();

            var readded = restoreContext.GetOrAddPackageReference("another-not-a-real-package-definitely-not", "5.7.1");
            readded.Should().BeNull();

            var result = await restoreContext.RestoreAsync();
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Packages_from_previous_requests_are_not_returned_in_subsequent_results()
        {
            using var restoreContext = new PackageRestoreContext();
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
        public async Task Can_get_path_to_nuget_packaged_assembly()
        {
            using var restoreContext = new PackageRestoreContext();
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
            using var restoreContext = new PackageRestoreContext();
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
            using var restoreContext = new PackageRestoreContext();
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
        public async Task Can_add_to_list_of_added_sources()
        {
            using var restoreContext = new PackageRestoreContext();
            restoreContext.AddRestoreSource("https://completely FakerestoreSource");
            await restoreContext.RestoreAsync();

            var restoreSources = restoreContext.RestoreSources;
            restoreSources.Should()
                          .ContainSingle("https://completely FakerestoreSource");
        }

        [Fact]
        public async Task Can_add_same_source_to_list_of_added_sources_without_error()
        {
            using var restoreContext = new PackageRestoreContext();
            restoreContext.AddRestoreSource("https://completely FakerestoreSource");
            restoreContext.AddRestoreSource("https://completely FakerestoreSource");

            await restoreContext.RestoreAsync();
            var restoreSources = restoreContext.RestoreSources;
            restoreSources.Should()
                          .ContainSingle("https://completely FakerestoreSource");
        }

        [Fact]
        public async Task Allows_duplicate_package_specifications()
        {
            using var restoreContext = new PackageRestoreContext();
            restoreContext.GetOrAddPackageReference("Microsoft.ML.AutoML", "0.16.0-preview");
            restoreContext.GetOrAddPackageReference("Microsoft.ML.AutoML", "0.16.0-preview");

            await restoreContext.RestoreAsync();

            var resolvedPackageReferences = restoreContext.ResolvedPackageReferences;
            resolvedPackageReferences.Should()
                                     .ContainSingle(r => r.PackageName == "Microsoft.ML.AutoML" && r.PackageVersion == "0.16.0-preview");
        }


        [Fact]
        // Question:   should it not throw, or is ignore sufficient
        public async Task Ignores__subsequent_package_specifications_with_different_higer_version()
        {
            using var restoreContext = new PackageRestoreContext();
            restoreContext.GetOrAddPackageReference("Microsoft.ML.AutoML", "0.16.0-preview");
            restoreContext.GetOrAddPackageReference("Microsoft.ML.AutoML", "0.16.1-preview");

            await restoreContext.RestoreAsync();

            var resolvedPackageReferences = restoreContext.ResolvedPackageReferences;
            resolvedPackageReferences.Should()
                                     .ContainSingle(r => r.PackageName == "Microsoft.ML.AutoML" && r.PackageVersion == "0.16.0-preview");
        }

        [Fact]
        public async Task Disallows_package_specifications_with_with_different_lower_version()
        {
            using var restoreContext = new PackageRestoreContext();
            restoreContext.GetOrAddPackageReference("Microsoft.ML.AutoML", "0.17.0-preview");
            restoreContext.GetOrAddPackageReference("Microsoft.ML.AutoML", "0.16.0-preview");
            await restoreContext.RestoreAsync();

            var resolvedPackageReferences = restoreContext.ResolvedPackageReferences;
            resolvedPackageReferences.Should()
                                     .ContainSingle(r => r.PackageName == "Microsoft.ML.AutoML" && r.PackageVersion == "0.17.0-preview");
        }

        [Fact]
        public async Task Disallows_package_specifications_with_with_different_lower_unspecified_version_first()
        {
            using var restoreContext = new PackageRestoreContext();
            restoreContext.GetOrAddPackageReference("Microsoft.ML.AutoML", "*");
            restoreContext.GetOrAddPackageReference("Microsoft.ML.AutoML", "0.16.0-preview");

            await restoreContext.RestoreAsync();
            var restoreSources = restoreContext.RestoreSources;

            var resolvedPackageReferences = restoreContext.ResolvedPackageReferences;
            resolvedPackageReferences.Should()
                                     .ContainSingle(r => r.PackageName == "Microsoft.ML.AutoML" && r.PackageVersion != "0.16.0-preview");
        }

        [Fact]
        public async Task Disallows_package_specifications_with_with_different_lower_unspecified_version_last()
        {
            using var restoreContext = new PackageRestoreContext();
            restoreContext.GetOrAddPackageReference("Microsoft.ML.AutoML", "0.16.0-preview");
            restoreContext.GetOrAddPackageReference("Microsoft.ML.AutoML", "*");

            await restoreContext.RestoreAsync();
            var restoreSources = restoreContext.RestoreSources;

            var resolvedPackageReferences = restoreContext.ResolvedPackageReferences;
            resolvedPackageReferences.Should()
                                     .ContainSingle(r => r.PackageName == "Microsoft.ML.AutoML" && r.PackageVersion == "0.16.0-preview");
        }
    }
}
