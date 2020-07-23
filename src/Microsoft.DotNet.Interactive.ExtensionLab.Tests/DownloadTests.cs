// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests
{
    public class DownloadTests
    {
        [Fact]
        public async Task it_downloads_a_file_to_the_current_directory_by_default()
        {
            using var kernel = new CompositeKernel { new CSharpKernel() };

            using var events = kernel.KernelEvents.ToSubscribedList();

            await new DownloadExtension().OnLoadAsync(kernel);

            var url = "https://raw.githubusercontent.com/dotnet/interactive/master/README.md";

            await kernel.SubmitCodeAsync($"#!download --uri {url}");

            var expectedFilePath = Path.Combine(Directory.GetCurrentDirectory(), "README.md");

            events.Should()
                  .NotContainErrors();

            File.Exists(expectedFilePath)
                .Should()
                .BeTrue();

            events.Should()
                  .ContainSingle<DisplayedValueProduced>(e => e.Value.As<string>() == $"Created file: {expectedFilePath}");
        }
        
        [Fact]
        public async Task it_downloads_a_file_to_the_specified_location()
        {
            using var kernel = new CompositeKernel { new CSharpKernel() };

            using var events = kernel.KernelEvents.ToSubscribedList();

            await new DownloadExtension().OnLoadAsync(kernel);

            var url = "https://raw.githubusercontent.com/dotnet/interactive/master/README.md";

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                Guid.NewGuid().ToString("N"),
                "a",
                Guid.NewGuid().ToString("N"));

            await kernel.SubmitCodeAsync($"#!download --uri {url} --output \"{path}\"");

            events.Should()
                  .NotContainErrors();

            File.Exists(path).Should().BeTrue();
        }
    }
}