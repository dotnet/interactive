// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    [LogTestNamesToPocketLogger]
    public class LanguageKernelExtensionLoadingTests : LanguageKernelTestBase
    {
        public LanguageKernelExtensionLoadingTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task It_loads_extensions_in_specified_directory_via_a_command()
        {
            var projectDir = DirectoryUtility.CreateDirectory();

            var extensionDir = projectDir
                .CreateSubdirectory("extension");

            var extensionDll = await KernelExtensionTestHelper.CreateExtensionInDirectory(
                                   projectDir,
                                   @"await kernel.SendAsync(new SubmitCode(""display(\""csharp extension installed\"");""));",
                                   extensionDir);

            var kernel = (IExtensibleKernel) CreateKernel(Language.CSharp);

            await using var context = KernelInvocationContext.Establish(new SubmitCode(""));

            using var events = context.KernelEvents.ToSubscribedList();

            await kernel.LoadExtensionsFromDirectoryAsync(
                extensionDir,
                context);

            events.Should()
                  .NotContain(e => e is CommandFailed)
                  .And
                  .ContainSingle<DisplayedValueUpdated>(dv => dv.Value.ToString().Contains(extensionDll.FullName));
        }

        [Fact]
        public async Task It_throws_when_extension_throws_during_load()
        {
            var projectDir = DirectoryUtility.CreateDirectory();

            var extensionDir = projectDir.CreateSubdirectory("extension");

            await KernelExtensionTestHelper.CreateExtensionInDirectory(
                projectDir,
                "throw new Exception();",
                extensionDir);

            var kernel = (IExtensibleKernel) CreateKernel(Language.CSharp);
            await using var context = KernelInvocationContext.Establish(new SubmitCode(""));

            using var events = context.KernelEvents.ToSubscribedList();

            await kernel.LoadExtensionsFromDirectoryAsync(
                extensionDir,
                context);

            events.Should()
                  .ContainSingle<CommandFailed>(cf => cf.Exception is KernelExtensionLoadException);
        }

        [Fact]
        public async Task It_loads_extensions_found_in_nuget_packages()
        {
            var directory = DirectoryUtility.CreateDirectory();

            const string packageName = "MyExtensionPackage";
            var packageVersion = "2.0.0";

            var nugetPackageDirectory = new DirectoryInfo(
                Path.Combine(
                    directory.FullName,
                    packageName,
                    packageVersion));

            var extensionsDir =
                new DirectoryInfo(
                    Path.Combine(
                        nugetPackageDirectory.FullName,
                        "interactive-extensions", "dotnet"));

            var extensionDll = await KernelExtensionTestHelper.CreateExtensionInDirectory(
                                   directory,
                                   @"await kernel.SendAsync(new SubmitCode(""using System.Reflection;""));",
                                   extensionsDir);

            var kernel = CreateKernel(Language.CSharp);

            await kernel.SubmitCodeAsync($"#r \"nuget:{packageName},{packageVersion}\"");

            KernelEvents.Should()
                        .ContainSingle<DisplayedValueUpdated>(e =>
                                                                  e.Value.ToString() == $"Loaded kernel extension TestKernelExtension from assembly {extensionDll.FullName}");
        }
    }
}