// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class KeyValueStoreKernelTests
    {
        [Fact]
        public async Task SubmitCode_is_not_valid_without_a_value_name()
        {
            using var kernel = CreateKernel();

            var storedValue = "1,2,3";

            var result = await kernel.SubmitCodeAsync(
                             @$"
#!value
{storedValue}
");

            using var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                  .ContainSingle<CommandFailed>()
                  .Which
                  .Message
                  .Should()
                  .Be("Option '--name' is required.");
        }

        [Fact]
        public async Task SubmitCode_stores_code_as_a_string()
        {
            using var kernel = CreateKernel();

            var storedValue = "1,2,3";

            await kernel.SubmitCodeAsync(
                @$"
#!value --name hi
{storedValue}");

            var keyValueStoreKernel = (DotNetKernel) kernel.FindKernel("value");

            keyValueStoreKernel.TryGetVariable("hi", out object retrievedValue);

            retrievedValue
                .Should()
                .BeOfType<string>()
                .Which
                .Should()
                .Be(storedValue);
        }

        [Fact]
        public async Task When_mime_type_is_specified_then_the_value_is_displayed_using_the_specified_mime_type()
        {
            using var kernel = CreateKernel();

            var storedValue = "1,2,3";

            var result = await kernel.SubmitCodeAsync(
                             @$"
#!value --name hello --mime-type text/test-stuff
{storedValue}
");

            using var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                  .ContainSingle<DisplayedValueProduced>()
                  .Which
                  .FormattedValues
                  .Should()
                  .ContainSingle(v => v.MimeType == "text/test-stuff" &&
                                      v.Value == storedValue);
        }

        [Theory]
        [InlineData("#!value --name hi --from-file {0}")]
        [InlineData("#!value --name hi --from-file {0}\n")]
        public async Task It_can_import_file_contents_as_strings(string code)
        {
            using var kernel = CreateKernel();

            var fileContents = "1,2,3";

            var filePath = Path.GetTempFileName();
            File.WriteAllText(filePath, fileContents);

            await kernel.SubmitCodeAsync(string.Format(code, filePath));

            var keyValueStoreKernel = (DotNetKernel) kernel.FindKernel("value");

            keyValueStoreKernel.TryGetVariable("hi", out object retrievedValue);

            retrievedValue
                .Should()
                .BeOfType<string>()
                .Which
                .Should()
                .Be(fileContents);
        }

        [Fact]
        public async Task It_can_import_URL_contents_as_strings()
        {
            using var kernel = CreateKernel();

            await kernel.SubmitCodeAsync("#!value --name hi --from-url http://bing.com");

            var keyValueStoreKernel = (DotNetKernel) kernel.FindKernel("value");

            keyValueStoreKernel.TryGetVariable("hi", out object retrievedValue);

            retrievedValue
                .Should()
                .BeOfType<string>()
                .Which
                .Should()
                .Contain("<html");
        }

        [Fact]
        public async Task from_file_and_from_url_options_are_mutually_exclusive()
        {
            using var kernel = CreateKernel();

            var result = await kernel.SubmitCodeAsync("#!value --name hi --from-url http://bing.com --from-file filename.txt");

            result.KernelEvents
                  .ToSubscribedList()
                  .Should()
                  .ContainSingle<CommandFailed>()
                  .Which
                  .Message
                  .Should()
                  .Be("The --from-url and --from-file options cannot be used together.");
        }

        [Fact]
        public async Task from_file_returns_error_when_content_is_also_submitted()
        {
            using var kernel = CreateKernel();

            var file = Path.GetTempFileName();
            File.WriteAllText(file, "1,2,3");

            var result = await kernel.SubmitCodeAsync($@"
#!value --name hi --from-file {file}
// some content");

            result.KernelEvents
                  .ToSubscribedList()
                  .Should()
                  .ContainSingle<CommandFailed>()
                  .Which
                  .Message
                  .Should()
                  .Be("The --from-file option cannot be used in combination with a content submission.");
        }

        [Fact]
        public async Task from_url_returns_error_when_content_is_also_submitted()
        {
            using var kernel = CreateKernel();

            var result = await kernel.SubmitCodeAsync($@"
#!value --name hi --from-url https://bing.com
// some content");

            result.KernelEvents
                  .ToSubscribedList()
                  .Should()
                  .ContainSingle<CommandFailed>()
                  .Which
                  .Message
                  .Should()
                  .Be("The --from-url option cannot be used in combination with a content submission.");
        }

        private static CompositeKernel CreateKernel() =>
            new CompositeKernel
            {
                new KeyValueStoreKernel(),
                new FakeKernel("#!fake")
            };
    }
}