// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Extensions;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.DotNet.Interactive.Tests.Utility.KernelExtensionTestHelper;

namespace Microsoft.DotNet.Interactive.Tests
{
    [LogTestNamesToPocketLogger]
    public class LanguageKernelExtensionLoadingTests : LanguageKernelTestBase
    {
        public LanguageKernelExtensionLoadingTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task It_loads_extensions_in_specified_directory_via_a_command(Language language)
        {
            var projectDir = DirectoryUtility.CreateDirectory();

            var dllDir = projectDir.CreateSubdirectory("extension");

            var code = language switch
            {
                Language.CSharp => @"await kernel.SendAsync(new SubmitCode(""display(\""C# extension installed\"");""));",
                Language.FSharp => @"await kernel.SendAsync(new SubmitCode(""display(\""F# extension installed\"");""));",
                _ => throw new NotSupportedException("This test does not support the specified language.")
            };

            var extensionDll = await CreateExtensionAssembly(
                                    projectDir,
                                    code,
                                    dllDir);

            var kernel = (IExtensibleKernel)CreateKernel(language);

            await using var context = KernelInvocationContext.Establish(new SubmitCode(""));

            using var events = context.KernelEvents.ToSubscribedList();

            await kernel.LoadExtensionsFromDirectoryAsync(
                dllDir,
                context);

            events.Should()
                    .NotContain(e => e is CommandFailed)
                    .And
                    .ContainSingle<DisplayedValueUpdated>(dv => dv.Value.ToString().Contains(extensionDll.FullName));
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task It_throws_when_extension_throws_during_load(Language language)
        {
            var projectDir = DirectoryUtility.CreateDirectory();

            var dllDir = projectDir.CreateSubdirectory("extension");

            await CreateExtensionAssembly(
                projectDir,
                "throw new Exception();",
                dllDir);

            var kernel = (IExtensibleKernel) CreateKernel(language);
            await using var context = KernelInvocationContext.Establish(new SubmitCode(""));

            using var events = context.KernelEvents.ToSubscribedList();

            await kernel.LoadExtensionsFromDirectoryAsync(
                dllDir,
                context);

            events.Should()
                  .ContainSingle<CommandFailed>(cf => cf.Exception is KernelExtensionLoadException);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task It_loads_extensions_found_in_nuget_packages(Language language)
        {
            var projectDir = DirectoryUtility.CreateDirectory();

            var packageName = $"MyTestExtension.{Path.GetRandomFileName()}";
            var packageVersion = "2.0.0-" + Guid.NewGuid().ToString("N");
            var guid = Guid.NewGuid().ToString();

            var nupkg = await CreateExtensionNupkg(
                            projectDir,
                            $"await kernel.SendAsync(new SubmitCode(\"\\\"{guid}\\\"\"));",
                            packageName,
                            packageVersion);

            var kernel = CreateKernel(language);

            await kernel.SubmitCodeAsync($@"
#i ""nuget:{nupkg.Directory.FullName}""
#r ""nuget:{packageName},{packageVersion}""            ");

            KernelEvents.Should()
                        .ContainSingle<ReturnValueProduced>()
                        .Which
                        .Value
                        .As<string>()
                        .Should()
                        .Contain(guid);
        }
    }
}