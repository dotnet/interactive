// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Http.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.LanguageServices;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Tests;

[TestClass]
public class KeyValueStoreKernelTests
{
    [TestMethod]
    public async Task SubmitCode_is_not_valid_without_a_value_name()
    {
        using var kernel = CreateKernel();

        var storedValue = "1,2,3";

        var result = await kernel.SubmitCodeAsync(
                         $"""
                          #!value
                          {storedValue}

                          """);

        result.Events.Should()
              .ContainSingle<CommandFailed>()
              .Which
              .Message
              .Should()
              .Be("(1,1): error DNI104: Missing required parameter '--name'");
    }

    [TestMethod]
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

    [TestMethod]
    public async Task When_mime_type_is_specified_then_the_value_is_displayed_using_the_specified_mime_type()
    {
        using var kernel = CreateKernel();

        var storedValue = "1,2,3";

        var result = await kernel.SubmitCodeAsync(
                         $"""
                          #!value --name hello --mime-type text/test-stuff
                          {storedValue}
                          """);

        result.Events
              .Should()
              .ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(v => v.MimeType == "text/test-stuff" &&
                                  v.Value == storedValue);
    }

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
    [DataRow("#!value --name hi --from-file {0}")]
    [DataRow("#!value --name hi --from-file {0}\n")]
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

    [TestMethod]
    public async Task It_can_import_URL_contents_as_strings()
    {
        var handler = new InterceptingHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent("<p>hi!</p>",
                                                 Encoding.UTF8,
                                                 "text/html");
            return Task.FromResult(response);
        });

        using var kernel = CreateKernel(new HttpClient(handler));

        await kernel.SubmitCodeAsync("#!value --name hi --from-url https://bing.com");

        var keyValueStoreKernel = kernel.FindKernelByName("value");

        var valueProduced = await keyValueStoreKernel.RequestValueAsync("hi");

        valueProduced.FormattedValue.Value
            .Should()
            .Contain("<p>hi!</p>");
    }

    [TestMethod]
    public async Task When_importing_URL_contents_the_mimetype_is_preserved()
    {
        var handler = new InterceptingHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent("<p>hi!</p>",
                                                 Encoding.UTF8,
                                                 "text/html");
            return Task.FromResult(response);
        });

        using var kernel = CreateKernel(new HttpClient(handler));

        await kernel.SubmitCodeAsync("#!value --name hi --from-url https://bing.com");

        var keyValueStoreKernel = kernel.FindKernelByName("value");

        var valueProduced = await keyValueStoreKernel.RequestValueAsync("hi");

        valueProduced.FormattedValue.MimeType
            .Should()
            .Be("text/html");
    }

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
    [Ignore("Consider changing this behavior")]
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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
    public async Task Completions_show_value_options()
    {
        using var kernel = CreateKernel();

        var markupCode = "#!value [||]".ParseMarkupCode();

        var result = await kernel.SendAsync(new RequestCompletions(markupCode.Code, new LinePosition(0, markupCode.Span.End)));

        var completionsProduced = result.Events.Should()
                                        .ContainSingle<CompletionsProduced>()
                                        .Which;
        
        completionsProduced
              .Completions
              .Select(c => c.InsertText)
              .Should()
              .Contain("--name", "--from-url", "--from-file", "--mime-type");
    }

    [TestMethod]
    public async Task Canceled_input_request_does_not_record_input_token_as_value()
    {
        using var kernel = CreateKernel();

        kernel.RegisterCommandHandler<RequestInput>((requestInput, context) =>
        {
            context.Fail(requestInput);
            return Task.CompletedTask;
        });

        kernel.SetDefaultTargetKernelNameForCommand(typeof(RequestInput), kernel.Name);

        var result = await kernel.SubmitCodeAsync(
                         """
                         #!value --from-value @input:{"type": "file"} --name file
                         """);

        result.Events.Should().ContainSingle<CommandFailed>();

        var keyValueStoreKernel = kernel.FindKernelByName("value");

        var (_, valueInfosProduced) = await keyValueStoreKernel.TryRequestValueInfosAsync();

        valueInfosProduced.ValueInfos.Should().BeEmpty();
    }

    private static CompositeKernel CreateKernel(HttpClient httpClient = null) =>
        new()
        {
            new KeyValueStoreKernel(httpClient: httpClient).UseValueSharing(),
            new FakeKernel("fake")
        };
}