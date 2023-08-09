// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.HttpRequest.Tests;

public class HttpRequestKernelTests
{
    public HttpRequestKernelTests()
    {
        Formatter.ResetToDefault();
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("POST")]
    [InlineData("DELETE")]
    [InlineData("HEAD")]
    public async Task supports_sending_requests_with_common_verbs(string verb)
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = message;
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpRequestKernel(client: client);

        var result = await kernel.SendAsync(new SubmitCode($"{verb} http://testuri.ninja"));

        using var _ = new AssertionScope();

        result.Events.Should().NotContainErrors();

        request.Method.Method.Should().Be(verb);
    }

    [Fact]
    public async Task it_can_interpolate_variable_for_URL_host()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpRequestKernel(client: client);

        using var _ = new AssertionScope();

        var result = await kernel.SendAsync(new SendValue("my_host", "my.host.com"));
        result.Events.Should().NotContainErrors();

        result = await kernel.SendAsync(new SubmitCode("get  https://{{my_host}}:1200/endpoint"));
        result.Events.Should().NotContainErrors();

        request.RequestUri.Should().Be("https://my.host.com:1200/endpoint");
    }

    [Fact]
    public async Task can_handle_multiple_request_in_a_single_submission()
    {
        List<HttpRequestMessage> requests = new();
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            requests.Add(message);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpRequestKernel(client: client);

        var result = await kernel.SendAsync(new SubmitCode("""
            
            get  https://location1.com:1200/endpoint
            ###
            put  https://location2.com:1200/endpoint
            """));

        using var _ = new AssertionScope();

        result.Events.Should().NotContainErrors();

        requests.Select(r => r.RequestUri.AbsoluteUri).ToArray().Should().BeEquivalentTo(new[] { "https://location1.com:1200/endpoint", "https://location2.com:1200/endpoint" });
    }

    [Fact]
    public async Task can_set_request_headers()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpRequestKernel(client: client);

        var result = await kernel.SendAsync(new SubmitCode("""
            
            get  https://location1.com:1200/endpoint
            Authorization: Basic username password
            """));

        using var _ = new AssertionScope();

        result.Events.Should().NotContainErrors();

        request.Headers.Authorization.ToString().Should().Be("Basic username password");
    }

    [Fact]
    public async Task can_set_body_from_single_line()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpRequestKernel(client: client);

        var result = await kernel.SendAsync(new SubmitCode("""
            
            post  https://location1.com:1200/endpoint
            Authorization: Basic username password
            Content-Type: application/json
            
            { "key" : "value", "list": [1, 2, 3] }
            """));

        using var _ = new AssertionScope();

        result.Events.Should().NotContainErrors();

        var bodyAsString = await request.Content.ReadAsStringAsync();
        bodyAsString.Should().Be("""{ "key" : "value", "list": [1, 2, 3] }""");
    }

    [Fact]
    public async Task can_set_body_from_multiline_text()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpRequestKernel(client: client);

        var result = await kernel.SendAsync(new SubmitCode("""
            post  https://location1.com:1200/endpoint
            Authorization: Basic username password
            Content-Type: application/json

            {
                "key" : "value",
                "list": [1, 2, 3]
            }

            """));

        using var _ = new AssertionScope();

        result.Events.Should().NotContainErrors();

        var bodyAsString = await request.Content.ReadAsStringAsync();
        bodyAsString.Should().BeExceptingWhitespace("""
            {
                "key" : "value",
                "list": [1, 2, 3]
            }
            """);
    }

    [Fact]
    public async Task can_set_contenttype_without_a_body()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpRequestKernel(client: client);

        var result = await kernel.SendAsync(new SubmitCode("""
            
            Get  https://location1.com:1200/endpoint
            Authorization: Basic username password
            Content-Type: application/json

            """));

        using var _ = new AssertionScope();
        result.Events.Should().NotContainErrors();
        request.Content.Headers.ContentType.ToString().Should().Be("application/json");
    }

    [Fact]
    public async Task can_use_symbols_in_body()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpRequestKernel(client: client);

        using var _ = new AssertionScope();

        var result = await kernel.SendAsync(new SendValue("one", 1));
        result.Events.Should().NotContainErrors();

        result = await kernel.SendAsync(new SubmitCode("""
            
            post  https://location1.com:1200/endpoint
            Authorization: Basic username password
            Content-Type: application/json
            
            { "key" : "value", "list": [{{one}}, 2, 3] }
            """));
        result.Events.Should().NotContainErrors();

        var bodyAsString = await request.Content.ReadAsStringAsync();
        bodyAsString.Should().Be("""{ "key" : "value", "list": [1, 2, 3] }""");
    }

    [Fact]
    public async Task comments_can_be_placed_before_a_variable_expanded_request()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpRequestKernel(client: client);

        using var _ = new AssertionScope();

        var result = await kernel.SendAsync(new SendValue("theHost", "example.com"));
        result.Events.Should().NotContainErrors();

        var code = """
            # something to ensure we're not on the first line
            GET https://{{theHost}}
            """;

        result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

        request.RequestUri.AbsoluteUri.Should().Be("https://example.com/");
    }

    [Fact]
    public async Task diagnostic_positions_are_correct_for_unresolved_symbols_in_URL()
    {
        using var kernel = new HttpRequestKernel();

        var result = await kernel.SendAsync(new RequestDiagnostics("get https://anotherlocation.com/{{api_endpoint}}"));

        using var _ = new AssertionScope();

        result.Events.Should().NotContainErrors();

        var diagnostics = result.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

        diagnostics.Diagnostics.First().Message.Should().Be("Cannot resolve symbol 'api_endpoint'");
    }

    [Fact]
    public async Task diagnostic_positions_are_correct_for_unresolved_symbols()
    {
        using var kernel = new HttpRequestKernel();

        var code = """
            
            // something to ensure we're not on the first line
            GET https://example.com/{{unresolved_symbol}}
            """;

        var result = await kernel.SendAsync(new RequestDiagnostics(code));

        using var _ = new AssertionScope();

        result.Events.Should().NotContainErrors();

        var diagnostics = result.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

        diagnostics.Diagnostics.Should().ContainSingle().Which.LinePositionSpan.Should().Be(new LinePositionSpan(new LinePosition(2, 26), new LinePosition(2, 43)));
    }

    [Fact]
    public async Task diagnostic_positions_are_correct_for_unresolved_symbols_after_other_symbols_were_successfully_resolved()
    {
        using var kernel = new HttpRequestKernel();

        var code = """
            
            GET https://example.com/
            User-Agent: {{unresolved_symbol}}
            """;

        var result = await kernel.SendAsync(new RequestDiagnostics(code));

        using var _ = new AssertionScope();

        result.Events.Should().NotContainErrors();

        var diagnostics = result.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

        diagnostics.Diagnostics.Should().ContainSingle().Which.LinePositionSpan.Should().Be(new LinePositionSpan(new LinePosition(2, 14), new LinePosition(2, 31)));
    }

    [Fact]
    public async Task multiple_diagnostics_are_returned_from_the_same_submission()
    {
        using var kernel = new HttpRequestKernel();

        var code = """
            
            GET {{missing_value_1}}/index.html
            User-Agent: {{missing_value_2}}
            """;

        var result = await kernel.SendAsync(new RequestDiagnostics(code));

        using var _ = new AssertionScope();

        result.Events.Should().NotContainErrors();

        var diagnostics = result.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

        diagnostics.Diagnostics.Should().HaveCount(2);
    }

    [Fact]
    public async Task when_error_diagnostics_are_present_then_request_is_not_sent()
    {
        var messageWasSent = false;
        var handler = new InterceptingHttpMessageHandler((_, _) =>
        {
            messageWasSent = true;
            throw new Exception();
        });
        var client = new HttpClient(handler);

        using var kernel = new HttpRequestKernel(client:client);

        await kernel.SendAsync(new SubmitCode("OOPS http://testuri.ninja"));

        messageWasSent.Should().BeFalse();
    }

    [Fact]
    public async Task produces_html_formatted_display_value()
    {
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = message;
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);

        using var root = new CompositeKernel();
        HttpRequestKernelExtension.Load(root, client);
        var kernel = root.FindKernels(k => k is HttpRequestKernel).Single();

        var result = await kernel.SendAsync(new SubmitCode($"GET http://testuri.ninja"));

        result.Events.Should().ContainSingle<DisplayedValueProduced>().Which
            .FormattedValues.Should().ContainSingle().Which
            .MimeType.Should().Be(HtmlFormatter.MimeType);
    }

    [Fact]
    public async Task produces_json_formatted_return_value()
    {
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = message;
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);

        using var root = new CompositeKernel();
        HttpRequestKernelExtension.Load(root, client);
        var kernel = root.FindKernels(k => k is HttpRequestKernel).Single();

        var result = await kernel.SendAsync(new SubmitCode($"GET http://testuri.ninja"));

        result.Events.Should().ContainSingle<ReturnValueProduced>().Which
            .FormattedValues.Should().ContainSingle().Which
            .MimeType.Should().Be(JsonFormatter.MimeType);
    }

    [Fact]
    public async Task display_should_be_suppressed_for_return_value()
    {
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = message;
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);

        using var root = new CompositeKernel();
        HttpRequestKernelExtension.Load(root, client);
        var kernel = root.FindKernels(k => k is HttpRequestKernel).Single();

        var result = await kernel.SendAsync(new SubmitCode($"GET http://testuri.ninja"));

        result.Events.Should().ContainSingle<ReturnValueProduced>().Which
            .FormattedValues.Should().ContainSingle().Which
            .SuppressDisplay.Should().BeTrue();
    }

    [Fact]
    public async Task produces_initial_displayed_value_that_is_updated_when_response_is_slow()
    {
        const int ResponseDelayThresholdInMilliseconds = 5;
        var slowResponseHandler = new InterceptingHttpMessageHandler(async (message, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = message;
            await Task.Delay(2 * ResponseDelayThresholdInMilliseconds);
            return response;
        });
        var client = new HttpClient(slowResponseHandler);

        using var root = new CompositeKernel();
        HttpRequestKernelExtension.Load(root, client, ResponseDelayThresholdInMilliseconds);
        var kernel = root.FindKernels(k => k is HttpRequestKernel).Single();

        var result = await kernel.SendAsync(new SubmitCode($"GET http://testuri.ninja"));

        using var _ = new AssertionScope();

        result.Events.Should().NotContainErrors();

        var displayEvents = result.Events.OfType<DisplayEvent>().ToArray();
        displayEvents.Length.Should().Be(3);
        displayEvents[0].Should().BeOfType<DisplayedValueProduced>();
        displayEvents[1].Should().BeOfType<DisplayedValueUpdated>();
        displayEvents[2].Should().BeOfType<ReturnValueProduced>();
    }

    [Fact]
    public async Task when_response_is_slow_initial_displayed_value_conveys_that_it_is_awaiting_response()
    {
        const int ResponseDelayThresholdInMilliseconds = 5;
        var slowResponseHandler = new InterceptingHttpMessageHandler(async (message, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = message;
            await Task.Delay(2 * ResponseDelayThresholdInMilliseconds);
            return response;
        });
        var client = new HttpClient(slowResponseHandler);

        using var root = new CompositeKernel();
        HttpRequestKernelExtension.Load(root, client, ResponseDelayThresholdInMilliseconds);
        var kernel = root.FindKernels(k => k is HttpRequestKernel).Single();

        var result = await kernel.SendAsync(new SubmitCode($"GET http://testuri.ninja"));

        result.Events.OfType<DisplayEvent>().First()
            .FormattedValues.Single().Value.Should().Contain("Awaiting response");
    }

    [Fact]
    public async Task when_response_is_slow_final_displayed_value_includes_response_details()
    {
        const int ResponseDelayThresholdInMilliseconds = 5;
        HttpRequestMessage request = null;
        var slowResponseHandler = new InterceptingHttpMessageHandler(async (message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = message;
            await Task.Delay(2 * ResponseDelayThresholdInMilliseconds);
            return response;
        });
        var client = new HttpClient(slowResponseHandler);

        using var root = new CompositeKernel();
        HttpRequestKernelExtension.Load(root, client, ResponseDelayThresholdInMilliseconds);
        var kernel = root.FindKernels(k => k is HttpRequestKernel).Single();

        var result = await kernel.SendAsync(new SubmitCode($"GET http://testuri.ninja"));

        result.Events.OfType<DisplayEvent>().Skip(1).First()
            .FormattedValues.Single().Value.Should().ContainAll("Response", "Request", "Headers");
    }

    [Fact]
    public async Task when_response_is_slow_and_an_error_happens_the_awaiting_response_displayed_value_is_cleared()
    {
        const int ResponseDelayThresholdInMilliseconds = 5;
        var throwingResponseHandler = new InterceptingHttpMessageHandler(async (message, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = message;
            await Task.Delay(2 * ResponseDelayThresholdInMilliseconds);
            throw new HttpRequestException();
        });
        var client = new HttpClient(throwingResponseHandler);

        using var root = new CompositeKernel();
        HttpRequestKernelExtension.Load(root, client, ResponseDelayThresholdInMilliseconds);
        var kernel = root.FindKernels(k => k is HttpRequestKernel).Single();

        var result = await kernel.SendAsync(new SubmitCode($"GET http://testuri.ninja"));
        var displayedValueUpdated = result.Events.OfType<DisplayedValueUpdated>().First();

        using var _ = new AssertionScope();

        displayedValueUpdated.Value.Should().Be(null);
        displayedValueUpdated.FormattedValues.Single(f => f.MimeType is HtmlFormatter.MimeType).Value.Should().Be("<span/>");
    }

    [Fact]
    public async Task produces_initial_displayed_value_that_is_updated_when_response_is_large()
    {
        const int ContentByteLengthThreshold = 100;

        var largeResponseHandler = new InterceptingHttpMessageHandler((message, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = message;
            var builder = new StringBuilder();
            for (int i = 0; i < ContentByteLengthThreshold + 1; ++i)
            {
                builder.Append('a');
            }
            response.Content = new StringContent(builder.ToString());
            return Task.FromResult(response);
        });

        var client = new HttpClient(largeResponseHandler);

        using var root = new CompositeKernel();
        HttpRequestKernelExtension.Load(root, client, contentByteLengthThreshold: ContentByteLengthThreshold);
        var kernel = root.FindKernels(k => k is HttpRequestKernel).Single();

        var result = await kernel.SendAsync(new SubmitCode($"GET http://testuri.ninja"));

        using var _ = new AssertionScope();

        result.Events.Should().NotContainErrors();

        var displayEvents = result.Events.OfType<DisplayEvent>().ToArray();
        displayEvents.Length.Should().Be(3);
        displayEvents[0].Should().BeOfType<DisplayedValueProduced>();
        displayEvents[1].Should().BeOfType<DisplayedValueUpdated>();
        displayEvents[2].Should().BeOfType<ReturnValueProduced>();
    }

    [Fact]
    public async Task when_response_is_large_initial_displayed_value_conveys_that_it_is_loading_response()
    {
        const int ContentByteLengthThreshold = 100;

        var largeResponseHandler = new InterceptingHttpMessageHandler((message, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = message;
            var builder = new StringBuilder();
            for (int i = 0; i < ContentByteLengthThreshold + 1; ++i)
            {
                builder.Append('a');
            }
            response.Content = new StringContent(builder.ToString());
            return Task.FromResult(response);
        });

        var client = new HttpClient(largeResponseHandler);

        using var root = new CompositeKernel();
        HttpRequestKernelExtension.Load(root, client, contentByteLengthThreshold: ContentByteLengthThreshold);
        var kernel = root.FindKernels(k => k is HttpRequestKernel).Single();

        var result = await kernel.SendAsync(new SubmitCode($"GET http://testuri.ninja"));

        result.Events.OfType<DisplayEvent>().First()
            .FormattedValues.Single().Value.Should().Contain("Loading content");
    }

    [Fact]
    public async Task when_response_is_large_final_displayed_value_includes_response_details()
    {
        const int ContentByteLengthThreshold = 100;

        var largeResponseHandler = new InterceptingHttpMessageHandler((message, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = message;
            var builder = new StringBuilder();
            for (int i = 0; i < ContentByteLengthThreshold + 1; ++i)
            {
                builder.Append('a');
            }
            response.Content = new StringContent(builder.ToString());
            return Task.FromResult(response);
        });

        var client = new HttpClient(largeResponseHandler);

        using var root = new CompositeKernel();
        HttpRequestKernelExtension.Load(root, client, contentByteLengthThreshold: ContentByteLengthThreshold);
        var kernel = root.FindKernels(k => k is HttpRequestKernel).Single();

        var result = await kernel.SendAsync(new SubmitCode($"GET http://testuri.ninja"));

        result.Events.OfType<DisplayEvent>().Skip(1).First()
            .FormattedValues.Single().Value.Should().ContainAll("Response", "Request", "Headers");
    }

    [Fact]
    public async Task produces_initial_displayed_value_that_is_updated_twice_when_response_is_slow_and_large()
    {
        const int ResponseDelayThresholdInMilliseconds = 5;
        const int ContentByteLengthThreshold = 100;

        var slowAndLargeResponseHandler = new InterceptingHttpMessageHandler(async (message, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = message;
            var builder = new StringBuilder();
            for (int i = 0; i < ContentByteLengthThreshold + 1; ++i)
            {
                builder.Append('a');
            }
            response.Content = new StringContent(builder.ToString());
            await Task.Delay(2 * ResponseDelayThresholdInMilliseconds);
            return response;
        });

        var client = new HttpClient(slowAndLargeResponseHandler);

        using var root = new CompositeKernel();
        HttpRequestKernelExtension.Load(root, client, ResponseDelayThresholdInMilliseconds, ContentByteLengthThreshold);
        var kernel = root.FindKernels(k => k is HttpRequestKernel).Single();

        var result = await kernel.SendAsync(new SubmitCode($"GET http://testuri.ninja"));

        using var _ = new AssertionScope();

        result.Events.Should().NotContainErrors();

        var displayEvents = result.Events.OfType<DisplayEvent>().ToArray();
        displayEvents.Length.Should().Be(4);
        displayEvents[0].Should().BeOfType<DisplayedValueProduced>();
        displayEvents[1].Should().BeOfType<DisplayedValueUpdated>();
        displayEvents[2].Should().BeOfType<DisplayedValueUpdated>();
        displayEvents[3].Should().BeOfType<ReturnValueProduced>();
    }

    [Fact]
    public async Task when_response_is_slow_and_large_first_displayed_value_conveys_that_it_is_awaiting_response()
    {
        const int ResponseDelayThresholdInMilliseconds = 5;
        const int ContentByteLengthThreshold = 100;

        var slowAndLargeResponseHandler = new InterceptingHttpMessageHandler(async (message, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = message;
            var builder = new StringBuilder();
            for (int i = 0; i < ContentByteLengthThreshold + 1; ++i)
            {
                builder.Append('a');
            }
            response.Content = new StringContent(builder.ToString());
            await Task.Delay(2 * ResponseDelayThresholdInMilliseconds);
            return response;
        });

        var client = new HttpClient(slowAndLargeResponseHandler);

        using var root = new CompositeKernel();
        HttpRequestKernelExtension.Load(root, client, ResponseDelayThresholdInMilliseconds, ContentByteLengthThreshold);
        var kernel = root.FindKernels(k => k is HttpRequestKernel).Single();

        var result = await kernel.SendAsync(new SubmitCode($"GET http://testuri.ninja"));

        result.Events.OfType<DisplayEvent>().First()
            .FormattedValues.Single().Value.Should().Contain("Awaiting response");
    }

    [Fact]
    public async Task when_response_is_slow_and_large_second_displayed_value_conveys_that_it_is_loading_response()
    {
        const int ResponseDelayThresholdInMilliseconds = 5;
        const int ContentByteLengthThreshold = 100;

        var slowAndLargeResponseHandler = new InterceptingHttpMessageHandler(async (message, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = message;
            var builder = new StringBuilder();
            for (int i = 0; i < ContentByteLengthThreshold + 1; ++i)
            {
                builder.Append('a');
            }
            response.Content = new StringContent(builder.ToString());
            await Task.Delay(2 * ResponseDelayThresholdInMilliseconds);
            return response;
        });

        var client = new HttpClient(slowAndLargeResponseHandler);

        using var root = new CompositeKernel();
        HttpRequestKernelExtension.Load(root, client, ResponseDelayThresholdInMilliseconds, ContentByteLengthThreshold);
        var kernel = root.FindKernels(k => k is HttpRequestKernel).Single();

        var result = await kernel.SendAsync(new SubmitCode($"GET http://testuri.ninja"));

        result.Events.OfType<DisplayEvent>().Skip(1).First()
            .FormattedValues.Single().Value.Should().Contain("Loading content");
    }

    [Fact]
    public async Task when_response_is_slow_and_large_final_displayed_value_includes_response_details()
    {
        const int ResponseDelayThresholdInMilliseconds = 5;
        const int ContentByteLengthThreshold = 100;

        var slowAndLargeResponseHandler = new InterceptingHttpMessageHandler(async (message, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.RequestMessage = message;
            var builder = new StringBuilder();
            for (int i = 0; i < ContentByteLengthThreshold + 1; ++i)
            {
                builder.Append('a');
            }
            response.Content = new StringContent(builder.ToString());
            await Task.Delay(2 * ResponseDelayThresholdInMilliseconds);
            return response;
        });

        var client = new HttpClient(slowAndLargeResponseHandler);

        using var root = new CompositeKernel();
        HttpRequestKernelExtension.Load(root, client, ResponseDelayThresholdInMilliseconds, ContentByteLengthThreshold);
        var kernel = root.FindKernels(k => k is HttpRequestKernel).Single();

        var result = await kernel.SendAsync(new SubmitCode("GET http://testuri.ninja"));

        result.Events.OfType<DisplayEvent>().Skip(2).First()
            .FormattedValues.Single().Value.Should().ContainAll("Response", "Request", "Headers");
    }

    [Fact(Skip = "Requires updates to HTTP parser")]
    public void responses_to_named_requests_can_be_accessed_as_symbols_in_later_requests()
    {
        // Request Variables
        // Request variables are similar to file variables in some aspects like scope and definition location.However, they have some obvious differences.The definition syntax of request variables is just like a single-line comment, and follows // @name requestName or # @name requestName just before the desired request url. 


        // TODO (responses_to_named_requests_can_be_accessed_as_symbols_in_later_requests) write test
        throw new NotImplementedException();
    }

    [Fact(Skip = "Requires updates to HTTP parser")]
    public void prompt_symbol_sends_input_request_to_user()
    {
        /*
###
# @prompt username
# @prompt refCode Your reference code display on webpage
# @prompt otp Your one-time password in your mailbox
POST https://{{host}}/verify-otp/{{refCode}} HTTP/1.1
Content-Type: {{contentType}}
{
    "username": "{{username}}",
    "otp": "{{otp}}"
}
         */

        // TODO (prompt_symbol_sends_input_request_to_user) write test
        throw new NotImplementedException();
    }

    [Fact(Skip = "Requires updates to HTTP parser")]
    public void JSONPath_can_be_used_to_access_response_properties()
    {
        // example:
        // @authToken = {{login.response.headers.X-AuthToken}}

        // TODO (dot_notation_can_be_used_to_access_response_properties) write test
        throw new NotImplementedException();
    }
}
