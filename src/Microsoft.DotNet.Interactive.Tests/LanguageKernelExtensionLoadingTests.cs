// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;
using static Microsoft.DotNet.Interactive.Tests.Utility.KernelExtensionTestHelper;

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

            await CreateExtensionAssembly(
                projectDir,
                code,
                dllDir);

            var kernel = CreateKernel(language);

            await using var context = KernelInvocationContext.Establish(new SubmitCode(""));

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

            await CreateExtensionAssembly(
                projectDir,
                "throw new Exception();",
                dllDir);

            var kernel = CreateKernel(language);
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
#r ""nuget:{packageName},{packageVersion}""");

            KernelEvents.Should()
                        .ContainSingle<ReturnValueProduced>()
                        .Which
                        .Value
                        .As<string>()
                        .Should()
                        .Contain(guid);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task It_does_not_try_to_load_the_same_extension_twice(Language language)
        {
            var projectDir = DirectoryUtility.CreateDirectory();

            var packageVersion = "2.0.0-" + Guid.NewGuid().ToString("N");

            var parent = await CreateExtensionNupkg(
                             projectDir,
                             "KernelInvocationContextExtensions.Display(KernelInvocationContext.Current, \"parent!\", \"text/plain\");",
                             $"MyTestExtension.{Path.GetRandomFileName()}" + ".Parent",
                             packageVersion);
            
            var child = await CreateExtensionNupkg(
                            projectDir,
                            "KernelInvocationContextExtensions.Display(KernelInvocationContext.Current, \"child!\", \"text/plain\");",
                            $"{$"MyTestExtension.{Path.GetRandomFileName()}"}.Child",
                            packageVersion);

            var kernel = CreateKernel(language);

            // FIX: (It_does_not_try_to_load_the_same_extension_twice) I think this test will only fail correctly if the second time the extension is loaded, it's a transitive dependency


            var code = $@"
#i ""nuget:{parent.Directory.FullName}""
#i ""nuget:{child.Directory.FullName}""
#r ""nuget:{$"MyTestExtension.{Path.GetRandomFileName()}"},{packageVersion}""";

            await kernel.SubmitCodeAsync(code);
            await kernel.SubmitCodeAsync(code);

            KernelEvents.Should().NotContainErrors();

            KernelEvents.Should()
                        .ContainSingle<DisplayedValueProduced>(v => v.Value == "hi!");

            // TODO-JOSEQU (It_does_not_try_to_load_the_same_extension_twice) write test
            Assert.True(false, "Test It_does_not_try_to_load_the_same_extension_twice is not written yet.");
        }
    }
}
