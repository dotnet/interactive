﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.DependencyManager;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class PackageRestoreContextTests : LanguageKernelTestBase
    {
        public PackageRestoreContextTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Returns_new_references_if_they_are_added(Language language)
        {
            var kernel = CreateBaseKernel(language) as ISupportNuget;
            using var restoreContext = new PackageRestoreContext(kernel);
            var added = restoreContext.GetOrAddPackageReference("FluentAssertions", "5.7.0");
            added.Should().NotBeNull();

            var result = await restoreContext.RestoreAsync();

            result.Errors.Should().BeEmpty();
            var assemblyPaths = result.ResolvedReferences.SelectMany(r => r.AssemblyPaths).ToArray();

            assemblyPaths.Should().Contain(r => r.Name.Equals("FluentAssertions.dll"));
            assemblyPaths.Should().Contain(r => r.Name.Equals("System.Configuration.ConfigurationManager.dll"));

            restoreContext
                .ResolvedPackageReferences
                .Should()
                .Contain(r => r.PackageName.Equals("FluentAssertions", StringComparison.OrdinalIgnoreCase) &&
                              r.PackageVersion == "5.7.0");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Returns_references_when_package_version_is_not_specified(Language language)
        {
            var kernel = CreateBaseKernel(language) as ISupportNuget;
            using var restoreContext = new PackageRestoreContext(kernel);
            var added = restoreContext.GetOrAddPackageReference("NewtonSoft.Json");
            added.Should().NotBeNull();

            var result = await restoreContext.RestoreAsync();

            result.Succeeded.Should().BeTrue();
            
            var assemblyPaths = result.ResolvedReferences.SelectMany(r => r.AssemblyPaths);
            assemblyPaths.Should().Contain(r => r.Name.Equals("NewtonSoft.Json.dll", StringComparison.InvariantCultureIgnoreCase));

            restoreContext
                .ResolvedPackageReferences
                .Should()
                .Contain(r => r.PackageName.Equals("NewtonSoft.Json", StringComparison.OrdinalIgnoreCase) &&
                              !string.IsNullOrWhiteSpace(r.PackageVersion));
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Returns_failure_if_package_installation_fails(Language language)
        {
            var kernel = CreateBaseKernel(language) as ISupportNuget;
            using var restoreContext = new PackageRestoreContext(kernel);
            var added = restoreContext.GetOrAddPackageReference("not-a-real-package-definitely-not", "5.7.0");
            added.Should().NotBeNull();

            var result = await restoreContext.RestoreAsync();
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Returns_failure_if_adding_package_twice_at_different_versions(Language language)
        {
            var kernel = CreateBaseKernel(language) as ISupportNuget;
            using var restoreContext = new PackageRestoreContext(kernel);
            var added = restoreContext.GetOrAddPackageReference("another-not-a-real-package-definitely-not", "5.7.0");
            added.Should().NotBeNull();

            var readded = restoreContext.GetOrAddPackageReference("another-not-a-real-package-definitely-not", "5.7.1");
            readded.Should().BeNull();

            var result = await restoreContext.RestoreAsync();
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Packages_from_previous_requests_are_not_returned_in_subsequent_results(Language language)
        {
            var kernel = CreateBaseKernel(language) as ISupportNuget;
            using var restoreContext = new PackageRestoreContext(kernel);
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

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Can_get_path_to_nuget_packaged_assembly(Language language)
        {
            var kernel = CreateBaseKernel(language) as ISupportNuget;
            using var restoreContext = new PackageRestoreContext(kernel);
            restoreContext.GetOrAddPackageReference("fluentAssertions", "5.7.0");

            await restoreContext.RestoreAsync();

            var packageReference = restoreContext.GetResolvedPackageReference("fluentassertions");

            var path = packageReference.AssemblyPaths.Single();

            // path is a string similar to:
            /// c:/users/someuser/.nuget/packages/fluentassertions/5.7.0/netcoreapp2.0/fluentassertions.dll
            var name = path.Name;
            var tfm = path.Directory.Name;
            var reflib = path.Directory.Parent.Name;
            var version = path.Directory.Parent.Parent.Name;
            var packageName = path.Directory.Parent.Parent.Parent.Name;

            name.ToLower().Should().Be("fluentassertions.dll");
            tfm.ToLower().Should().Be("netcoreapp2.0");
            reflib.ToLower().Should().Be("lib");
            version.ToLower().Should().Be("5.7.0");
            packageName.ToLower().Should().Be("fluentassertions");
            path.Exists.Should().BeTrue();
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Can_get_path_to_nuget_package_root(Language language)
        {
            var kernel = CreateBaseKernel(language) as ISupportNuget;
            using var restoreContext = new PackageRestoreContext(kernel);
            restoreContext.GetOrAddPackageReference("fluentAssertions", "5.7.0");

            await restoreContext.RestoreAsync();

            var packageReference = restoreContext.GetResolvedPackageReference("fluentassertions");

            var path = packageReference.PackageRoot;

            var version = path.Name;
            var packageName = path.Parent.Name;
            version.ToLower().Should().Be("5.7.0");
            packageName.ToLower().Should().Be("fluentassertions");
            path.Exists.Should().BeTrue();
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Can_get_path_to_nuget_package_when_multiple_packages_are_added(Language language)
        {
            var kernel = CreateBaseKernel(language) as ISupportNuget;
            using var restoreContext = new PackageRestoreContext(kernel);
            restoreContext.GetOrAddPackageReference("fluentAssertions", "5.7.0");
            restoreContext.GetOrAddPackageReference("htmlagilitypack", "1.11.12");

            await restoreContext.RestoreAsync();

            var packageReference = restoreContext.GetResolvedPackageReference("htmlagilitypack");

            var path = packageReference.AssemblyPaths.Single();

            path.FullName.ToLower()
                .Should()
                .EndWith("htmlagilitypack" + Path.DirectorySeparatorChar +
                         "1.11.12" + Path.DirectorySeparatorChar +
                         "lib" + Path.DirectorySeparatorChar +
                         "netstandard2.0" + Path.DirectorySeparatorChar +
                         "htmlagilitypack.dll");
            path.Exists.Should().BeTrue();
        }

        // TODO: (PackageRestoreContextTests) add the same package twice
        // TODO: (PackageRestoreContextTests) add the same package twice, once with version specified and once unspecified
        // TODO: (PackageRestoreContextTests) add the same package twice, lower version then higher version
        // TODO: (PackageRestoreContextTests) add the same package twice, higher version then lower version
    }
}
