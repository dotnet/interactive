// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Parsing;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
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
                Language.CSharp => $@"await kernel.SendAsync(new SubmitCode(""display(\""{language} extension installed\"");""));",
                Language.FSharp => $@"await kernel.SendAsync(new SubmitCode(""display(\""{language} extension installed\"");""));",
                _ => throw new NotSupportedException("This test does not support the specified language.")
            };

            await KernelExtensionTestHelper.CreateExtensionAssembly(
                projectDir,
                code,
                dllDir);

            var kernel = CreateKernel(language);

            using var context = KernelInvocationContext.Establish(new SubmitCode(""));

            using var events = context.KernelEvents.ToSubscribedList();

            await kernel.LoadExtensionsFromDirectoryAsync(
                dllDir,
                context);

            events.Should()
                  .NotContain(e => e is CommandFailed)
                  .And
                  .ContainSingle<DisplayedValueProduced>(v => v.FormattedValues.Single().Value == $"{language} extension installed");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task It_throws_when_extension_throws_during_load(Language language)
        {
            var projectDir = DirectoryUtility.CreateDirectory();

            var dllDir = projectDir.CreateSubdirectory("extension");

            await KernelExtensionTestHelper.CreateExtensionAssembly(
                projectDir,
                "throw new Exception();",
                dllDir);

            var kernel = CreateKernel(language);
            using var context = KernelInvocationContext.Establish(new SubmitCode(""));

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

            var extensionPackage = KernelExtensionTestHelper.GetOrCreateSimpleExtension();

            var kernel = CreateKernel(language);

            await kernel.SubmitCodeAsync($@"
#i ""nuget:{extensionPackage.PackageLocation}""
#r ""nuget:{extensionPackage.Name},{extensionPackage.Version}""");

            KernelEvents.Should()
                        .ContainSingle<ReturnValueProduced>()
                        .Which
                        .Value
                        .As<string>()
                        .Should()
                        .Contain("SimpleExtension");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_loads_script_extension_found_in_nuget_package(Language defaultLanguage)
        {
            var extensionPackage = KernelExtensionTestHelper.GetOrCreateScriptBasedExtensionPackage();

            var kernel = CreateCompositeKernel(defaultLanguage);

            await kernel.SubmitCodeAsync($@"
#i ""nuget:{extensionPackage.PackageLocation}""
#r ""nuget:{extensionPackage.Name},{extensionPackage.Version}""");

            KernelEvents.Should()
                        .ContainSingle<ReturnValueProduced>()
                        .Which
                        .Value
                        .As<string>()
                        .Should()
                        .Contain("ScriptExtension");
        }
    }
}
