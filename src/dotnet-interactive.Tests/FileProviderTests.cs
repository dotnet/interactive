// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

using FluentAssertions;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class FileProviderTests : LanguageKernelTestBase
    {


        public FileProviderTests(ITestOutputHelper output) : base(output)
        {
        }

     

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public void It_loads_content_from_root_provider(Language language)
        {
            var kernel = CreateKernel(language);
            var provider = new FileProvider(kernel, typeof(Program).Assembly);

            var file = provider.GetFileInfo("resources/dotnet-interactive.js");

            file.Should()
                .NotBeNull();
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task It_does_not_track_extensions_that_are_not_file_providers(Language language)
        {

            var kernel = CreateKernel(language);
            var provider = new FileProvider(kernel, typeof(Program).Assembly);

            var extensionPackage = KernelExtensionTestHelper.GetOrCreateSimpleExtension();

            await kernel.SubmitCodeAsync($@"
#i ""nuget:{extensionPackage.PackageLocation}""
#r ""nuget:{extensionPackage.Name},{extensionPackage.Version}""            ");

            Action action = () => provider.GetFileInfo("extensions/TestKernelExtension/resources/file.txt");

            action.Should().Throw<StaticContentSourceNotFoundException>();
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task It_tracks_extensions_that_are_not_file_providers(Language language)
        {

            var kernel = CreateKernel(language);
            var provider = new FileProvider(kernel, typeof(Program).Assembly);

            var extensionPackage = KernelExtensionTestHelper.GetOrCreateFileProviderExtension();

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

            action.Should().Throw<StaticContentSourceNotFoundException>();
        }
    }
}