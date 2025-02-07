// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.App.Tests;

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

        var kernel = CreateCompositeKernel(language);

        using var context = KernelInvocationContext.GetOrCreateAmbientContext(new SubmitCode(""));

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

        var kernel = CreateCompositeKernel(language);
        using var context = KernelInvocationContext.GetOrCreateAmbientContext(new SubmitCode(""));

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
        var extensionPackage = await KernelExtensionTestHelper.GetSimpleExtensionAsync();

        var kernel = CreateCompositeKernel(language);

        await kernel.SubmitCodeAsync(
            $"""

             #i "nuget:{extensionPackage.PackageLocation}"
             #r "nuget:{extensionPackage.Name},{extensionPackage.Version}"
             """);

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
        var extensionPackage = await KernelExtensionTestHelper.GetScriptExtensionPackageAsync();

        var kernel = CreateCompositeKernel(defaultLanguage);

        var result = await kernel.SubmitCodeAsync(
                         $"""
                          #i "nuget:{extensionPackage.PackageLocation}"
                          #r "nuget:{extensionPackage.Name},{extensionPackage.Version}"
                          """);

        result.Events.Should().NotContainErrors();

        KernelEvents.Should()
                    .ContainSingle<ReturnValueProduced>()
                    .Which
                    .Value
                    .As<string>()
                    .Should()
                    .Contain("ScriptExtension loaded");
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task It_does_not_track_extensions_that_are_not_file_providers(Language language)
    {
        var kernel = CreateKernel(language);
        var provider = new FileProvider(kernel, typeof(Program).Assembly);

        var extensionPackage = await KernelExtensionTestHelper.GetSimpleExtensionAsync();

        await kernel.SubmitCodeAsync($@"
#i ""nuget:{extensionPackage.PackageLocation}""
#r ""nuget:{extensionPackage.Name},{extensionPackage.Version}""            ");

        Action action = () => provider.GetFileInfo("extensions/TestKernelExtension/resources/file.txt");

        action.Should().Throw<KeyNotFoundException>();
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task It_tracks_extensions_that_are_not_file_providers(Language language)
    {
        var kernel = CreateCompositeKernel(language);
        var provider = new FileProvider(kernel, typeof(Program).Assembly);

        var extensionPackage = await KernelExtensionTestHelper.GetFileProviderExtensionAsync();

        await kernel.SubmitCodeAsync($@"
#i ""nuget:{extensionPackage.PackageLocation}""
#r ""nuget:{extensionPackage.Name},{extensionPackage.Version}""            ");

        var file = provider.GetFileInfo("extensions/TestKernelExtension/resources/file.txt");

        file.Should()
            .NotBeNull();
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public void it_cannot_resolve_unregistered_extensions(Language language)
    {
        var kernel = CreateKernel(language);
        var provider = new FileProvider(kernel, typeof(Program).Assembly);

        Action action
            = () => provider.GetFileInfo("extensions/not_found/resources/file.txt");

        action.Should().Throw<KeyNotFoundException>();
    }

    protected override CompositeKernel CreateCompositeKernel(Language defaultKernelLanguage = Language.CSharp, bool openTestingNamespaces = false)
    {
        var kernel = base.CreateCompositeKernel(defaultKernelLanguage, openTestingNamespaces);
        kernel.UseNuGetExtensions();
        return kernel;
    }
}