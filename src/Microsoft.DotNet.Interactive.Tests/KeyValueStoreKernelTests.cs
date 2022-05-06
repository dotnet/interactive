// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.LanguageServices;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.ValueSharing;
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

            var keyValueStoreKernel = (ISupportGetValue) kernel.FindKernel("value");

            keyValueStoreKernel.TryGetValue("hi", out object retrievedValue);

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
            await File.WriteAllTextAsync(filePath, fileContents);

            await kernel.SubmitCodeAsync(string.Format(code, filePath));

            var keyValueStoreKernel = (ISupportGetValue) kernel.FindKernel("value");

            keyValueStoreKernel.TryGetValue("hi", out object retrievedValue);

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

            var keyValueStoreKernel = (ISupportGetValue) kernel.FindKernel("value");

            keyValueStoreKernel.TryGetValue("hi", out object retrievedValue);

            retrievedValue
                .Should()
                .BeOfType<string>()
                .Which
                .Should()
                .Contain("<html");
        }

        [Fact]
        public async Task It_can_store_user_input()
        {
            using var kernel = CreateKernel();
            
            kernel.RegisterCommandHandler<RequestInput>((requestInput, context) =>
            {
                context.Publish(new InputProduced("hello!", requestInput));
                return Task.CompletedTask;
            });

            kernel.SetDefaultTargetKernelNameForCommand(typeof(RequestInput), kernel.Name);
            await kernel.SubmitCodeAsync("#!value --name hi --from-value @input:input-please");

            var keyValueStoreKernel = (ISupportGetValue)kernel.FindKernel("value");

            keyValueStoreKernel.TryGetValue("hi", out object retrievedValue);

            retrievedValue
                .Should()
                .BeOfType<string>()
                .Which
                .Should()
                .Be("hello!");
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
        public async Task from_file_and_from_value_options_are_mutually_exclusive()
        {
            using var kernel = CreateKernel();

            var result = await kernel.SubmitCodeAsync("#!value --name hi --from-file filename.txt --from-value x");

            result.KernelEvents
                  .ToSubscribedList()
                  .Should()
                  .ContainSingle<CommandFailed>()
                  .Which
                  .Message
                  .Should()
                  .Be("The --from-value and --from-file options cannot be used together.");
        }

        [Fact]
        public async Task from_url_and_from_value_options_are_mutually_exclusive()
        {
            using var kernel = CreateKernel();

            var result = await kernel.SubmitCodeAsync("#!value --name hi --from-url http://bing.com --from-value x");

            result.KernelEvents
                  .ToSubscribedList()
                  .Should()
                  .ContainSingle<CommandFailed>()
                  .Which
                  .Message
                  .Should()
                  .Be("The --from-url and --from-value options cannot be used together.");
        }

        [Fact]
        public async Task Share_into_the_value_kernel_is_not_supported_and_stores_the_directive_text_literally()
        {
            using var kernel = CreateKernel();

            var result = await kernel.SubmitCodeAsync(@"
#!value --name x
#!share --from fsharp f
");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            var valueKernel = (KeyValueStoreKernel)kernel.FindKernel("value");

            valueKernel.TryGetValue<string>("x", out var x).Should().BeTrue();

            x.Should().Be("#!share --from fsharp f");
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

            var url = $"http://example.com/{Guid.NewGuid():N}";
            var result = await kernel.SubmitCodeAsync($@"
#!value --name hi --from-url {url}
// some content");

            result.KernelEvents.ToSubscribedList()
                .Should()
                .ContainSingle<CommandFailed>()
                .Which
                .Message
                .Should()
                .Be("The --from-url option cannot be used in combination with a content submission.");
        }

        [Fact]
        public async Task when_from_url_is_used_with_content_then_the_response_is_not_stored()
        {
            using var kernel = CreateKernel();

            var url = $"http://example.com/{Guid.NewGuid():N}";
            var result = await kernel.SubmitCodeAsync($@"
#!value --name hi --from-url {url}
// some content");

            result.KernelEvents.ToSubscribedList();

            kernel
                .FindKernel("value")
                .As<ISupportGetValue>()
                .GetValueInfos()
                .Should()
                .NotContain(vi => vi.Name == "hi");
        }

        [Fact]
        public async Task when_from_url_is_used_with_content_then_the_previous_value_is_retained()
        {
            using var kernel = CreateKernel();

            var url = $"http://example.com/{Guid.NewGuid():N}";

            await kernel.SubmitCodeAsync($@"
#!value --name hi 
// previous content");

            var result = await kernel.SubmitCodeAsync($@"
#!value --name hi --from-url {url}
// some content");

            result.KernelEvents.ToSubscribedList();

            kernel
                .FindKernel("value")
                .As<ISupportGetValue>()
                .TryGetValue("hi", out object previousValue)
                .Should()
                .BeTrue();

            previousValue.Should()
                .Be("// previous content");
        }

        [Fact]
        public async Task Completions_show_value_options()
        {
            using var kernel = CreateKernel();

            var markupCode = "#!value [||]".ParseMarkupCode();

            var result = await kernel.SendAsync(new RequestCompletions(markupCode.Code, new LinePosition(0, markupCode.Span.End)));

            var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                  .ContainSingle<CompletionsProduced>()
                  .Which
                  .Completions
                  .Select(c => c.InsertText)
                  .Should()
                  .Contain("--name", "--from-url", "--from-file", "--mime-type");
        }

        private static CompositeKernel CreateKernel() =>
            new()
            {
                new KeyValueStoreKernel(),
                new FakeKernel("fake")
            };
    }
}