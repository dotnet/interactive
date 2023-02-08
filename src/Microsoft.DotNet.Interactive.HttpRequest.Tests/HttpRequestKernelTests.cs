// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.HttpRequest.Tests;

public class HttpRequestKernelTests
{
    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("POST")]
    [InlineData("DELETE")]
    [InlineData("HEAD")]
    public async Task supports_verbs(string verb)
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

        result.Events.Should().NotContainErrors();

        request.Method.Method.Should().Be(verb);

    }

    [Fact]
    public async Task requires_base_address_when_using_relative_uris()
    {
        using var kernel = new HttpRequestKernel();

        var result = await kernel.SendAsync(new SubmitCode("get  /relativePath"));

        var error =  result.Events.Should().ContainSingle<CommandFailed>().Which;

        error.Message.Should().Contain("Cannot use relative path /relativePath without a base address.");
    }

    [Fact]
    public async Task ignores_base_address_when_using_absolute_paths()
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
        kernel.BaseAddress = new Uri("http://example.com");
        
        var result = await kernel.SendAsync(new SubmitCode("get  https://anotherlocation.com/endpoint"));

        result.Events.Should().NotContainErrors();

        request.RequestUri.Should().Be("https://anotherlocation.com/endpoint");
    }

    [Fact]
    public async Task can_replace_symbols()
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
     
        kernel.SetValue("my_host", "my.host.com");

        var result = await kernel.SendAsync(new SubmitCode("get  https://{{my_host}}:1200/endpoint"));

        result.Events.Should().NotContainErrors();

        request.RequestUri.Should().Be("https://my.host.com:1200/endpoint");
    }

    [Fact]
    public async Task can_use_base_address_to_resolve_host_symbol()
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
        kernel.BaseAddress = new Uri("http://example.com");

        var result = await kernel.SendAsync(new SubmitCode("get  https://{{host}}:1200/endpoint"));

        result.Events.Should().NotContainErrors();

        request.RequestUri.Should().Be("https://example.com:1200/endpoint");
    }

    [Fact]
    public async Task can_handle_multiple_request_in_a_single_submission()
    {
        List<HttpRequestMessage> requests = new ();
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            requests.Add(message);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpRequestKernel(client: client);

        var result = await kernel.SendAsync(new SubmitCode(@"
get  https://location1.com:1200/endpoint

put  https://location2.com:1200/endpoint"));

        result.Events.Should().NotContainErrors();

        requests.Select(r => r.RequestUri.AbsoluteUri).ToArray().Should().BeEquivalentTo(new []{ "https://location1.com:1200/endpoint", "https://location2.com:1200/endpoint" });
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

        var result = await kernel.SendAsync(new SubmitCode(@"
get  https://location1.com:1200/endpoint
Authorization: Basic username password"));

        result.Events.Should().NotContainErrors();

        request.Headers.Authorization.ToString().Should().Be("Basic username password");
    }

    [Fact]
    public async Task can_set_body()
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

        var result = await kernel.SendAsync(new SubmitCode(@"
post  https://location1.com:1200/endpoint
Authorization: Basic username password
Content-Type: application/json

{ ""key"" : ""value"", ""list"": [1, 2, 3] }
"));

        result.Events.Should().NotContainErrors();

        var bodyAsString = await request.Content.ReadAsStringAsync();
        bodyAsString.Should().Be("{ \"key\" : \"value\", \"list\": [1, 2, 3] }");
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
        kernel.SetValue("one","1");
        var result = await kernel.SendAsync(new SubmitCode(@"
post  https://location1.com:1200/endpoint
Authorization: Basic username password
Content-Type: application/json

{ ""key"" : ""value"", ""list"": [{{one}}, 2, 3] }
"));

        result.Events.Should().NotContainErrors();

        var bodyAsString = await request.Content.ReadAsStringAsync();
        bodyAsString.Should().Be("{ \"key\" : \"value\", \"list\": [1, 2, 3] }");
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
        kernel.SetValue("theHost", "example.com");

        var code = @"
// something to ensure we're not on the first line
GET https://{{theHost}}";

        var result = await kernel.SendAsync(new SubmitCode(code));

        result.Events.Should().NotContainErrors();

        request.RequestUri.AbsoluteUri.Should().Be("https://example.com/");
    }

    [Fact]
    public async Task diagnostic_messages_are_produced_for_unresolved_symbols()
    {
        using var kernel = new HttpRequestKernel();
        kernel.BaseAddress = new Uri("http://example.com");

        var result = await kernel.SendAsync(new RequestDiagnostics("get https://anotherlocation.com/{{api_endpoint}}"));

        result.Events.Should().NotContainErrors();

        var diagnostics = result.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

        diagnostics.Diagnostics.First().Message.Should().Be(@"Cannot resolve symbol 'api_endpoint'");
    }

    [Fact]
    public async Task diagnostic_positions_are_correct_for_unresolved_symbols()
    {
        using var kernel = new HttpRequestKernel();
        kernel.BaseAddress = new Uri("http://example.com");

        var code = @"
// something to ensure we're not on the first line
GET https://example.com/{{unresolved_symbol}}";

        var result = await kernel.SendAsync(new RequestDiagnostics(code));

        result.Events.Should().NotContainErrors();

        var diagnostics = result.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

        diagnostics.Diagnostics.Should().ContainSingle().Which.LinePositionSpan.Should().Be(new LinePositionSpan(new LinePosition(2, 26), new LinePosition(2, 43)));
    }

    [Fact]
    public async Task Setting_BaseAddress_sets_host_variable()
    {
        using var kernel = new HttpRequestKernel();
        kernel.BaseAddress = new Uri("http://example.com");

        var result = await kernel.SendAsync(new RequestValue("host"));

        result.Events.Should().NotContainErrors();

        result.Events.Should().ContainSingle<ValueProduced>()
              .Which.Value.Should().Be("example.com");
    }

    [Fact]
    public async Task diagnostic_positions_are_correct_for_unresolved_symbols_after_other_symbols_were_successfully_resolved()
    {
        using var kernel = new HttpRequestKernel();
        kernel.BaseAddress = new Uri("http://example.com");

        var code = @"
GET {{host}}/index.html
User-Agent: {{user_agent}}";

        var result = await kernel.SendAsync(new RequestDiagnostics(code));

        result.Events.Should().NotContainErrors();

        var diagnostics = result.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

        diagnostics.Diagnostics.Should().ContainSingle().Which.LinePositionSpan.Should().Be(new LinePositionSpan(new LinePosition(2, 14), new LinePosition(2, 24)));
    }

    [Fact]
    public async Task multiple_diagnostics_are_returned_from_the_same_submission()
    {
        using var kernel = new HttpRequestKernel();
        kernel.BaseAddress = new Uri("http://example.com");

        var code = @"
GET {{missing_value_1}}/index.html
User-Agent: {{missing_value_2}}";

        var result = await kernel.SendAsync(new RequestDiagnostics(code));

        result.Events.Should().NotContainErrors();

        var diagnostics = result.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

        diagnostics.Diagnostics.Should().HaveCount(2);
    }
}
