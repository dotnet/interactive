// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.DotNet.Interactive.App.Http;
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
            var provider = new FileProvider(kernel);

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
            var provider = new FileProvider(kernel);

            var projectDir = DirectoryUtility.CreateDirectory();

            var packageName = $"MyTestExtension.{Path.GetRandomFileName()}";
            var packageVersion = "2.0.0-" + Guid.NewGuid().ToString("N");
            var guid = Guid.NewGuid().ToString();

            var nupkg = await KernelExtensionTestHelper.CreateExtensionNupkg(
                projectDir,
                $"await kernel.SendAsync(new SubmitCode(\"\\\"{guid}\\\"\"));",
                packageName,
                packageVersion);



            await kernel.SubmitCodeAsync($@"
#i ""nuget:{nupkg.Directory.FullName}""
#r ""nuget:{packageName},{packageVersion}""            ");

            var file = provider.GetFileInfo("extensions/TestKernelExtension/resources/file.txt");

            file.Should()
                .NotBeNull();
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task It_tracks_extensions_that_are_not_file_providers(Language language)
        {

            var kernel = CreateKernel(language);
            var provider = new FileProvider(kernel);

            var projectDir = DirectoryUtility.CreateDirectory();
            var fileToEmbed = new FileInfo(Path.Combine(projectDir.FullName, "file.txt"));
            File.WriteAllText(fileToEmbed.FullName, "for testing only");
            var packageName = $"MyTestExtension.{Path.GetRandomFileName()}";
            var packageVersion = "2.0.0-" + Guid.NewGuid().ToString("N");
            var guid = Guid.NewGuid().ToString();

            var nupkg = await KernelExtensionTestHelper.CreateExtensionNupkg(
                projectDir,
                $"await kernel.SendAsync(new SubmitCode(\"\\\"{guid}\\\"\"));",
                packageName,
                packageVersion,
                fileToEmbed);



            await kernel.SubmitCodeAsync($@"
#i ""nuget:{nupkg.Directory.FullName}""
#r ""nuget:{packageName},{packageVersion}""            ");

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
            var provider = new FileProvider(kernel);

            var file = provider.GetFileInfo("extensions/not_found/resources/file.txt");

            file.Should()
                .NotBeNull();
        }
    }
}