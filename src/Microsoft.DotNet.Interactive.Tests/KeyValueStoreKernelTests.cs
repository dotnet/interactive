// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.LanguageServices;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

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

        result.Events.Should()
              .ContainSingle<CommandFailed>()
              .Which
              .Message
              .Should()
              .Be("Option '--name' is required.");
    }

    [Fact]
    public async Task SubmitCode_stores_code_as_a_formatted_value()
    {
        using var kernel = CreateKernel();

        var storedValue = "1,2,3";

        await kernel.SubmitCodeAsync(
            @$"
#!value --name hi
{storedValue}");

        var keyValueStoreKernel = kernel.FindKernelByName("value");

        var valueProduced = await keyValueStoreKernel.RequestValueAsync("hi");

        valueProduced.FormattedValue.Value
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

        result.Events
              .Should()
              .ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(v => v.MimeType == "text/test-stuff" &&
                                  v.Value == storedValue);
    }

    [Fact]
    public async Task When_mime_type_is_specified_then_it_retains_the_specified_mime_type()
    {
        using var kernel = CreateKernel();

        var storedValue = "1,2,3";

        await kernel.SubmitCodeAsync(
            @$"
#!value --name hello --mime-type text/test-stuff
{storedValue}
");

        var result = await kernel.SendAsync(new RequestValue("hello", targetKernelName:"value"));

        result.Events
              .Should()
              .ContainSingle<ValueProduced>()
              .Which
              .FormattedValue
              .MimeType
              .Should()
              .Be("text/test-stuff");
    }

    [Fact]
    public async Task requestValueInfos_uses_mimetypes_as_type_names()
    {
        using var kernel = CreateKernel();

        var storedValue = "1,2,3";

        await kernel.SubmitCodeAsync(
            $"""
                #!value --name a --mime-type text/plain
                {storedValue}

                """);


        await kernel.SubmitCodeAsync(
            $"""
                #!value --name b --mime-type application/json
                {storedValue}

                """);

        var result = await kernel.SendAsync(new RequestValueInfos( targetKernelName: "value"));

        var valueInfosProduced = result.Events.Should()
                                       .ContainSingle<ValueInfosProduced>()
                                       .Which;

        valueInfosProduced.ValueInfos.Should().ContainSingle(v => v.Name == "a" && v.TypeName == "text/plain");

        valueInfosProduced.ValueInfos.Should().ContainSingle(v => v.Name == "b" && v.TypeName == "application/json");
    }

    [Fact]
    public async Task When_mime_type_is_not_specified_then_it_default_to_text_plain()
    {
        using var kernel = CreateKernel();

        var storedValue = "1,2,3";

        await kernel.SubmitCodeAsync(
            $"""
                #!value --name hello
                {storedValue}
                """);

        var result = await kernel.SendAsync(new RequestValue("hello", targetKernelName: "value"));

        result.Events.Should()
              .ContainSingle<ValueProduced>()
              .Which
              .FormattedValue
              .MimeType
              .Should()
              .Be("text/plain");
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

        var keyValueStoreKernel = kernel.FindKernelByName("value");

        var valueProduced = await keyValueStoreKernel.RequestValueAsync("hi");

        valueProduced.FormattedValue.Value
            .Should()
            .Be(fileContents);
    }

    [Fact]
    public async Task It_can_import_URL_contents_as_strings()
    {
        using var kernel = CreateKernel();

        await kernel.SubmitCodeAsync("#!value --name hi --from-url https://bing.com");

        var keyValueStoreKernel = kernel.FindKernelByName("value");

        var valueProduced = await keyValueStoreKernel.RequestValueAsync("hi");

        valueProduced.FormattedValue.Value
            .Should()
            .Contain("<html");
    }

    [Fact]
    public async Task When_import_URL_contents_the_mimetype_is_preserved()
    {
        using var kernel = CreateKernel();

        await kernel.SubmitCodeAsync("#!value --name hi --from-url https://bing.com");

        var keyValueStoreKernel = kernel.FindKernelByName("value");

        var valueProduced = await keyValueStoreKernel.RequestValueAsync("hi");

        valueProduced.FormattedValue.MimeType
            .Should()
            .Be("text/html");
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

        var keyValueStoreKernel = kernel.FindKernelByName("value");

        var valueProduced = await keyValueStoreKernel.RequestValueAsync("hi");

        valueProduced.FormattedValue.Value
            .Should()
            .Be("hello!");
    }

    [Fact]
    public async Task from_file_and_from_url_options_are_mutually_exclusive()
    {
        using var kernel = CreateKernel();

        var result = await kernel.SubmitCodeAsync("#!value --name hi --from-url http://bing.com --from-file filename.txt");

        result.Events
            .Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .Be("(1,46): error DNI205: The --from-url and --from-file options cannot be used together.");
    }

    [Fact]
    public async Task from_file_and_from_value_options_are_mutually_exclusive()
    {
        using var kernel = CreateKernel();

        var result = await kernel.SubmitCodeAsync("#!value --name hi --from-file filename.txt --from-value x");

        result.Events
            .Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .Be("(1,44): error DNI207: The --from-value and --from-file options cannot be used together.");
    }

    [Fact]
    public async Task from_url_and_from_value_options_are_mutually_exclusive()
    {
        using var kernel = CreateKernel();

        var result = await kernel.SubmitCodeAsync("#!value --name hi --from-url http://bing.com --from-value x");

        result.Events
            .Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .Be("(1,46): error DNI206: The --from-url and --from-value options cannot be used together.");
    }

    [Fact]
    public async Task Share_into_the_value_kernel_is_not_supported_and_stores_the_directive_text_literally()
    {
        using var kernel = CreateKernel();

        var result = await kernel.SubmitCodeAsync("""
            #!value --name x
            #!share --from fsharp f
            """);

        result.Events.Should().NotContainErrors();

        var valueKernel = (KeyValueStoreKernel)kernel.FindKernelByName("value");

        var valueProduced = await valueKernel.RequestValueAsync("x");

        valueProduced.FormattedValue.Value.Should().Be("#!share --from fsharp f");
    }

    [Fact]
    public async Task from_file_returns_error_when_content_is_also_submitted()
    {
        using var kernel = CreateKernel();

        var file = Path.GetTempFileName();
        await File.WriteAllTextAsync(file, "1,2,3");

        var result = await kernel.SubmitCodeAsync($"""
            #!value --name hi --from-file {file}
            // some content
            """);

        result.Events
            .Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .Be("(1,19): error DNI208: The --from-file option cannot be used in combination with a content submission.");
    }

    [Fact]
    public async Task from_url_returns_error_when_content_is_also_submitted()
    {
        using var kernel = CreateKernel();

        var url = $"http://example.com/{Guid.NewGuid():N}";
        var result = await kernel.SubmitCodeAsync($"""
            #!value --name hi --from-url {url}
            // some content
            """);

        result.Events
            .Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .Be("(1,19): error DNI209: The --from-url option cannot be used in combination with a content submission.");
    }

    [Fact]
    public async Task Multiple_value_kernel_invocations_can_be_submitted_together_when_from_options_are_used()
    {
        using var kernel = CreateKernel();

        var file = Path.GetTempFileName();
        await File.WriteAllTextAsync(file, "hello from file");
        var url = $"http://example.com/{Guid.NewGuid():N}";

        var result = await kernel.SubmitCodeAsync($@"
#!value --name file --from-file {file}
#!value --name url --from-url {url}
#!value --name inline --from-value ""hello from value""

// some content

");

        using var _ = new AssertionScope();

        result.Events.Should().NotContainErrors();

        var valueKernel = kernel.ChildKernels.OfType<KeyValueStoreKernel>().Single();

        valueKernel.Values.Should().ContainKey("file");
        valueKernel.Values.Should().ContainKey("url");
        valueKernel.Values.Should().ContainKey("inline");
    }

    [Fact]
    public async Task when_from_url_is_used_with_content_then_the_response_is_not_stored()
    {
        using var kernel = CreateKernel();

        var url = $"http://example.com/{Guid.NewGuid():N}";
        await kernel.SubmitCodeAsync($"""
            #!value --name hi --from-url {url}
            // some content
            """);

        var valueKernel = kernel.FindKernelByName("value");

        var (success, valueInfosProduced) = await valueKernel.TryRequestValueInfosAsync();

        valueInfosProduced.ValueInfos
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

        await kernel.SubmitCodeAsync($@"
#!value --name hi --from-url {url}
// some content");

        var valueKernel = kernel.FindKernelByName("value");

        var valueProduced = await valueKernel.RequestValueAsync("hi");
        
        valueProduced.FormattedValue.Value
            .Should()
            .Be("// previous content");
    }

    [Fact]
    public async Task Completions_show_value_options()
    {
        using var kernel = CreateKernel();

        var markupCode = "#!value [||]".ParseMarkupCode();

        var result = await kernel.SendAsync(new RequestCompletions(markupCode.Code, new LinePosition(0, markupCode.Span.End)));

        result.Events.Should()
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