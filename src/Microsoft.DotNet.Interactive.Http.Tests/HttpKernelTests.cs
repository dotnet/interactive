// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;

namespace Microsoft.DotNet.Interactive.Http.Tests;

public class HttpKernelTests
{
    public HttpKernelTests()
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
        using var kernel = new HttpKernel(client: client);

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
        using var kernel = new HttpKernel(client: client);

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
        using var kernel = new HttpKernel(client: client);

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
        using var kernel = new HttpKernel(client: client);

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
        using var kernel = new HttpKernel(client: client);

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
        using var kernel = new HttpKernel(client: client);

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
    public async Task invalid_header_value_produces_error()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        var result = await kernel.SendAsync(new SubmitCode("""
            Get  https://location1.com:1200/endpoint
            Date: OOPS!
            """));

        using var _ = new AssertionScope();
        result.Events.OfType<CommandFailed>().Single().Message.Should().EndWith(
            "The format of value 'OOPS!' is invalid.");
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
        using var kernel = new HttpKernel(client: client);

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
        using var kernel = new HttpKernel(client: client);

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
    public async Task binding_in_variable_value_is_valid()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            @hostname=httpbin.org
            @host=https://{{hostname}}

            Get {{host}}
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));

        result.Events.Should().NotContainErrors();
        request.RequestUri.Should().Be($"https://httpbin.org");


    }

    [Fact]
    public async Task can_bind_guid()
    {
        HttpRequestMessage request = null;

        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);
        _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments

            {
                "request_id": "{{$guid}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

        var bodyAsString = await request.Content.ReadAsStringAsync();
        var guidSubstring = bodyAsString.Split(":").Last().Trim().Substring(1);
        var guidString = guidSubstring.Substring(0, guidSubstring.IndexOf("\""));
        Guid.TryParse(guidString, out _).Should().BeTrue();

    }

    [Theory]
    [InlineData("$guid$guid")]
    [InlineData("$dateTime$dateTime")]
    [InlineData("$timestamp$timestamp")]
    [InlineData("$localDateTime$localDateTime")]
    [InlineData("$randomInt$randomInt")]
    [InlineData("$randomInt$guid")]
    public async Task cant_bind_multiples_in_expression(string expression)
    {
        HttpRequestMessage request = null;

        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = $$$"""
            POST https://api.example.com/comments

            {
                "request_id": "{{{{{expression}}}}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));


        var diagnostics = result.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

        diagnostics.Diagnostics.First().Message.Should().Be($"Unable to evaluate expression '{expression}'.");

    }

    [Theory]
    [InlineData("$GUID")]
    [InlineData("$TIMESTAMP")]
    [InlineData("$DATETIME")]
    [InlineData("$LOCALDATETIME")]
    [InlineData("$RANDOMINT")]
    public async Task cant_bind_capital_versions_of_expressions(string expression)
    {
        HttpRequestMessage request = null;

        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = $$$"""
            POST https://api.example.com/comments

            {
                "request_id": "{{{{{expression}}}}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));

        var diagnostics = result.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

        diagnostics.Diagnostics.First().Message.Should().Be($"Unable to evaluate expression '{expression}'.");


    }

    [Fact]
    public async Task can_bind_timestamp()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments
            
            {
                "updated_at" : "{{$timestamp}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

        var bodyAsString = await request.Content.ReadAsStringAsync();
        var unixValueSubstring = bodyAsString.Split(":").Last().Trim().Substring(1);
        var unixValueString = unixValueSubstring.Substring(0, unixValueSubstring.IndexOf("\""));
        var unixValue = DateTimeOffset.FromUnixTimeSeconds(long.Parse(unixValueString));
        unixValue.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("+1")]
    [InlineData("0")]
    [InlineData("4")]
    public async Task can_bind_timestamp_with_valid_offset(string offsetDays)
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = $$$"""
            POST https://api.example.com/comments
            
            {
                "created_at" : "{{$timestamp {{{offsetDays}}} d}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

        var bodyAsString = await request.Content.ReadAsStringAsync();
        var unixValueSubstring = bodyAsString.Split(":").Last().Trim().Substring(1);
        var unixValueString = unixValueSubstring.Substring(0, unixValueSubstring.IndexOf("\""));
        var unixValue = DateTimeOffset.FromUnixTimeSeconds(long.Parse(unixValueString));
        var offsetDaysInteger = int.Parse(offsetDays);
        var dateTimeOffset = DateTimeOffset.UtcNow.AddDays(offsetDaysInteger);
        unixValue.Should().BeCloseTo(dateTimeOffset, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task cant_bind_timestamp_offset_without_option()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = $$$"""
            POST https://api.example.com/comments
            
            {
                "created_at" : "{{$timestamp -1}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));

        var diagnostics = result.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

        diagnostics.Diagnostics.First().Message.Should().Be("Unable to evaluate expression '$timestamp -1'.");
    }

    [Fact]
    public async Task cant_bind_timestamp_offset_with_invalid_option()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = $$$"""
            POST https://api.example.com/comments
            
            {
                "created_at" : "{{$timestamp -1 q}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));

        var diagnostics = result.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

        diagnostics.Diagnostics.First().Message.Should().Be("The supplied option 'q' in the expression '$timestamp -1 q' is not supported.");
    }



    [Fact]
    public async Task cant_bind_timestamp_offset_with_invalid_offset()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = $$$"""
            POST https://api.example.com/comments
            
            {
                "created_at" : "{{$timestamp 33.2 d}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));

        var diagnostics = result.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

        diagnostics.Diagnostics.First().Message.Should().Be("The supplied offset '33.2' in the expression '$timestamp 33.2 d' is not a valid integer.");
    }

    [Fact]
    public async Task cant_bind_timestamp_with_invalid_chars_in_the_arguments()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = $$$"""
            POST https://api.example.com/comments
            
            {
                "created_at" : "{{$timestamp ~1 d}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));

        result.Events.Should().ContainSingle<DiagnosticsProduced>().Which.Diagnostics.Should().ContainSingle().Which.Message.Should().Be("The supplied offset '~1' in the expression '$timestamp ~1 d' is not a valid integer.");
    }

    [Fact]
    public async Task can_bind_random_int_with_no_arguments()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);
        _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments
            
            {
                "review_count" : "{{$randomInt}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

        var bodyAsString = await request.Content.ReadAsStringAsync();
        var randIntSubstring = bodyAsString.Split(":").Last().Trim().Substring(1);
        var randIntValue = randIntSubstring.Substring(0, randIntSubstring.IndexOf("\""));

        int.TryParse(randIntValue, out _).Should().BeTrue();
    }

    [Fact]
    public async Task can_bind_random_int_with_only_max()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments
            
            {
                "review_count" : "{{$randomInt 10}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

        var bodyAsString = await request.Content.ReadAsStringAsync();
        var randIntSubstring = bodyAsString.Split(":").Last().Trim().Substring(1);
        var randIntValue = randIntSubstring.Substring(0, randIntSubstring.IndexOf("\""));


        int intValueOfRandInt = int.Parse(randIntValue);

        intValueOfRandInt.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public async Task can_bind_random_int_with_arguments()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments
            
            {
                "review_count" : "{{$randomInt 10 99}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

        var bodyAsString = await request.Content.ReadAsStringAsync();
        var randIntSubstring = bodyAsString.Split(":").Last().Trim().Substring(1);
        var randIntValue = randIntSubstring.Substring(0, randIntSubstring.IndexOf("\""));


        int intValueOfRandInt = int.Parse(randIntValue);

        intValueOfRandInt.Should().BeGreaterThanOrEqualTo(10);
        intValueOfRandInt.Should().BeLessThanOrEqualTo(99);
    }

    [Fact]
    public async Task can_bind_random_int_with_negative_values()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments
            
            {
                "review_count" : "{{$randomInt -10 99}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

        var bodyAsString = await request.Content.ReadAsStringAsync();
        var randIntSubstring = bodyAsString.Split(":").Last().Trim().Substring(1);
        var randIntValue = randIntSubstring.Substring(0, randIntSubstring.IndexOf("\""));

        int intValueOfRandInt = int.Parse(randIntValue);

        intValueOfRandInt.Should().BeGreaterThanOrEqualTo(-10);
        intValueOfRandInt.Should().BeLessThanOrEqualTo(99);
    }

    [Fact]
    public async Task cant_bind_random_int_with_min_greater_than_max()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments
            
            {
                "review_count" : "{{$randomInt 99 10}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().ContainSingle<DiagnosticsProduced>().Which.Diagnostics.Should().ContainSingle().Which.Message.Should().Be("""The supplied argument '99' in the expression '$randomInt 99 10' must not be greater than the supplied argument '10'.""");
    }

    [Fact]
    public async Task cant_bind_random_int_with_non_integer_max()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments
            
            {
                "review_count" : "{{$randomInt 10 99.3}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().ContainSingle<DiagnosticsProduced>().Which.Diagnostics.Should().ContainSingle().Which.Message.Should().Be("The supplied argument '99.3' in the expression '$randomInt 10 99.3' is not a valid integer.");
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("+1")]
    [InlineData("0")]
    [InlineData("4")]
    public async Task can_bind_datetime_with_valid_offset(string offsetDays)
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = $$$"""
            POST https://api.example.com/comments
            
            {
                "created_at" : "{{$datetime 'yyyy-MM-dd hh:mm:ss tt' {{{offsetDays}}} d}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

        var bodyAsString = await request.Content.ReadAsStringAsync();
        var dateTimeString = bodyAsString.Split("\"created_at\" : ").Last().Trim(new[] { '\r', '\n', '{', '}', '"' });
        var dateTimeValue = DateTime.Parse(dateTimeString);
        var offsetDaysInteger = int.Parse(offsetDays);
        var dateTimeOffset = DateTime.UtcNow.AddDays(offsetDaysInteger);
        dateTimeValue.Should().BeCloseTo(dateTimeOffset, TimeSpan.FromSeconds(10));
    }

    [Theory]
    [InlineData("h")]
    [InlineData("y")]
    [InlineData("ms")]
    [InlineData("d")]
    [InlineData("s")]
    [InlineData("M")]
    [InlineData("w")]
    [InlineData("m")]
    public async Task binding_with_various_datetime_options_doesnt_produce_errors(string option)
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = $$$"""
            POST https://api.example.com/comments
            
            {
                "created_at" : "{{$datetime 'yyyy-MM-dd' -1 {{{option}}}}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();
    }

    [Fact]
    public async Task invalid_option_produces_error()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments
            
            {
                "created_at" : "{{$datetime 'yyyy-MM-dd' -1 t}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().ContainSingle<DiagnosticsProduced>().Which.Diagnostics.Should().ContainSingle().Which.Message.Should().Be("The supplied option 't' in the expression '$datetime 'yyyy-MM-dd' -1 t' is not supported.");
    }

    [Fact]
    public async Task can_bind_datetime_with_arguments()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments
            
            {
                "custom_date" : "{{$datetime 'yyyy-MM-dd'}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

        var currentDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var bodyAsString = await request.Content.ReadAsStringAsync();
        var readDateSubstring = bodyAsString.Split(":").Last().Trim().Substring(1);
        var readDateValue = readDateSubstring.Substring(0, readDateSubstring.IndexOf("\""));
        readDateValue.Should().BeEquivalentTo(currentDate);

    }

    [Fact]
    public async Task can_bind_datetime_with_offset()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments
          
            {
                "custom_date" : "{{$datetime 'yyyy-MM-dd' -1 d}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

        var offsetDate = DateTime.UtcNow.AddDays(-1.0).ToString("yyyy-MM-dd");
        var bodyAsString = await request.Content.ReadAsStringAsync();
        var readDateSubstring = bodyAsString.Split(":").Last().Trim().Substring(1);
        var readDateValue = readDateSubstring.Substring(0, readDateSubstring.IndexOf("\""));

        readDateValue.Should().BeEquivalentTo(offsetDate);

    }

    [Fact]
    public async Task can_bind_datetime_with_no_arguments()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments
          
            {
                "custom_date" : "{{$datetime}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

        var offsetDate = DateTime.Now;
        var bodyAsString = await request.Content.ReadAsStringAsync();
        var readDateSubstring = bodyAsString.Split("\"custom_date\" : ").Last().Trim().Substring(1);
        var readDateValue = readDateSubstring.Substring(0, readDateSubstring.IndexOf("\""));

        DateTime.Parse(readDateValue).Should().BeCloseTo(offsetDate, TimeSpan.FromSeconds(10));

    }

    [Fact]
    public async Task can_bind_datetime_with_offset_and_default_format()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments
          
            {
                "date_offset" : "{{$datetime -1 d}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

        var offsetDate = DateTime.Now.AddDays(-1.0);
        var bodyAsString = await request.Content.ReadAsStringAsync();
        var readDateSubstring = bodyAsString.Split("\"date_offset\" : ").Last().Trim().Substring(1);
        var readDateValue = readDateSubstring.Substring(0, readDateSubstring.IndexOf("\""));

        DateTime.Parse(readDateValue).Should().BeCloseTo(offsetDate, TimeSpan.FromSeconds(10));

    }

    [Fact]
    public async Task can_bind_local_datetime_with_arguments()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments

            {
                "local_custom_date" : "{{$localDatetime 'yyyy-MM-dd'}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

        var currentDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var bodyAsString = await request.Content.ReadAsStringAsync();
        var readDateSubstring = bodyAsString.Split(":").Last().Trim().Substring(1);
        var readDateValue = readDateSubstring.Substring(0, readDateSubstring.IndexOf("\""));
        readDateValue.Should().BeEquivalentTo(currentDate);
    }

    [Fact]
    public async Task can_bind_local_datetime_with_iso_format()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments

            {
                "local_custom_date" : "{{$localDatetime iso8601}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

        var currentDate = DateTimeOffset.UtcNow;
        var bodyAsString = await request.Content.ReadAsStringAsync();
        var readDateSubstring = bodyAsString.Split("\"local_custom_date\" : ").Last().Trim().Substring(1);
        var readDateValue = readDateSubstring.Substring(0, readDateSubstring.IndexOf("\""));
        DateTimeOffset.Parse(readDateValue).Should().BeCloseTo(currentDate, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task can_bind_local_datetime_with_rfc_format()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments

            {
                "local_custom_date" : "{{$localDatetime rfc1123}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

        var currentDate = DateTimeOffset.UtcNow;
        var bodyAsString = await request.Content.ReadAsStringAsync();
        var readDateSubstring = bodyAsString.Split("\"local_custom_date\" : ").Last().Trim().Substring(1);
        var readDateValue = readDateSubstring.Substring(0, readDateSubstring.IndexOf("\""));
        DateTimeOffset.Parse(readDateValue).Should().BeCloseTo(currentDate, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task several_system_variables_in_single_request()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments="{{$randomInt 5 200}}"
            Current-time: "{{$timestamp}}"
            
            {
                "request_id": "{{$guid}}",
                "updated_at": "{{$timestamp}}",
                "created_at": "{{$timestamp -1 d}}",
                "custom_date": "{{$datetime 'yyyy-MM-dd'}}",
                "local_custom_date": "{{$localDatetime 'yyyy-MM-dd'}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

    }

    [Fact]
    public async Task url_in_embedded_expression_is_valid()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            @hostname=httpbin.org
            @host=https://{{hostname}}
            POST {{host}}/anything
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));
        result.Events.Should().NotContainErrors();

        request.RequestUri.AbsoluteUri.Should().Be("https://httpbin.org/anything");
    }

    [Fact]
    public async Task incorrect_datetime_syntax_produces_error()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments
            
            {
                "local_custom_date": "{{$localDatetime 'YYYY-NN-DD}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));

        result.Events.Should().ContainSingle<DiagnosticsProduced>().Which.Diagnostics.Should().ContainSingle().Which.Message.Should().Be("Unable to evaluate expression '$localDatetime 'YYYY-NN-DD'.");

    }


    [Fact]
    public async Task incorrect_datetime_produces_error()
    {
        HttpRequestMessage request = null;
        var handler = new InterceptingHttpMessageHandler((message, _) =>
        {
            request = message;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(response);
        });
        var client = new HttpClient(handler);
        using var kernel = new HttpKernel(client: client);

        using var _ = new AssertionScope();

        var code = """
            POST https://api.example.com/comments
            
            {
                "local_custom_date": "{{$localDatetime 'YYYY-MM-DD'}}"
            }
            """;

        var result = await kernel.SendAsync(new SubmitCode(code));

        result.Events.Should().ContainSingle<DiagnosticsProduced>().Which.Diagnostics.Should().ContainSingle().Which.Message.Should().Be("""The supplied expression '$localDatetime 'YYYY-MM-DD'' does not follow the correct pattern. The expression should adhere to the following pattern: '{$localDatetime [rfc1123|iso8601|"custom format"] [offset option]}' where offset (if specified) must be a valid integer and option must be one of the following: ms, s, m, h, d, w, M, Q, y. See https://aka.ms/http-date-time-format for more details.""");

    }

    [Fact]
    public async Task diagnostic_positions_are_correct_for_unresolved_symbols_in_URL()
    {
        using var kernel = new HttpKernel();

        var result = await kernel.SendAsync(new RequestDiagnostics("get https://anotherlocation.com/{{api_endpoint}}"));

        using var _ = new AssertionScope();

        result.Events.Should().NotContainErrors();

        var diagnostics = result.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

        diagnostics.Diagnostics.First().Message.Should().Be("Unable to evaluate expression 'api_endpoint'.");
    }

    [Fact]
    public async Task diagnostic_positions_are_correct_for_unresolved_symbols()
    {
        using var kernel = new HttpKernel();

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
        using var kernel = new HttpKernel();

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
        using var kernel = new HttpKernel();

        var code = """
            
            GET http://{{missing_value_1}}/index.html
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

        using var kernel = new HttpKernel(client: client);

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

        using var kernel = new HttpKernel(client: client);

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

        using var kernel = new HttpKernel("http", client);

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

        using var kernel = new HttpKernel("http", client);

        var result = await kernel.SendAsync(new SubmitCode("GET http://testuri.ninja"));

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

        using var kernel = new HttpKernel("http", client, ResponseDelayThresholdInMilliseconds);

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

        using var kernel = new HttpKernel("http", client, ResponseDelayThresholdInMilliseconds);

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

        using var kernel = new HttpKernel("http", client, ResponseDelayThresholdInMilliseconds);

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

        using var kernel = new HttpKernel("http", client, ResponseDelayThresholdInMilliseconds);

        var result = await kernel.SendAsync(new SubmitCode("GET http://testuri.ninja"));
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
        using var kernel = new HttpKernel("http", client, contentByteLengthThreshold: ContentByteLengthThreshold);

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

        using var kernel = new HttpKernel("http", client, contentByteLengthThreshold: ContentByteLengthThreshold);

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

        using var kernel = new HttpKernel("http", client, contentByteLengthThreshold: ContentByteLengthThreshold);

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

        using var kernel = new HttpKernel("http", client, ResponseDelayThresholdInMilliseconds, ContentByteLengthThreshold);

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

        using var kernel = new HttpKernel("http", client, ResponseDelayThresholdInMilliseconds, ContentByteLengthThreshold);

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

        using var kernel = new HttpKernel("http", client, ResponseDelayThresholdInMilliseconds, ContentByteLengthThreshold);

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

        using var kernel = new HttpKernel("http", client, ResponseDelayThresholdInMilliseconds, ContentByteLengthThreshold);

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

    [Fact]
    public async Task It_supports_RequestValueInfos()
    {
        using var kernel = new HttpKernel();

        var sendValueResult = await kernel.SendAsync(new SendValue("theValue", 123, FormattedValue.CreateSingleFromObject(123, JsonFormatter.MimeType)));

        sendValueResult.Events.Should().NotContainErrors();

        var result = await kernel.SendAsync(new RequestValueInfos());

        using var _ = new AssertionScope();
        result.Events.Should().NotContainErrors();
        var valueInfo = result.Events.Should().ContainSingle<ValueInfosProduced>()
                              .Which
                              .ValueInfos.Should().ContainSingle()
                              .Which;
        valueInfo.Name.Should().Be("theValue");
        valueInfo.FormattedValue.Should().BeEquivalentTo(new FormattedValue(PlainTextSummaryFormatter.MimeType, "123"));
    }

    [Fact]
    public async Task It_supports_RequestValue()
    {
        using var kernel = new HttpKernel();

        var sendValueResult = await kernel.SendAsync(new SendValue("theValue", 123, FormattedValue.CreateSingleFromObject(123, JsonFormatter.MimeType)));

        sendValueResult.Events.Should().NotContainErrors();

        var result = await kernel.SendAsync(new RequestValue("theValue", JsonFormatter.MimeType));

        using var _ = new AssertionScope();
        var valueProduced = result.Events.Should().ContainSingle<ValueProduced>()
                                  .Which;
        valueProduced.Name.Should().Be("theValue");
        valueProduced
            .FormattedValue.Should()
            .BeEquivalentTo(new FormattedValue(JsonFormatter.MimeType, "123"));
    }

    [Fact] // https://github.com/dotnet/interactive/issues/3239
    public async Task traceparent_header_has_a_new_top_level_value_for_each_request()
    {
        using var kernel = new HttpKernel();

        // the app often creates a parent activity
        using var activity = new Activity(MethodBase.GetCurrentMethod().Name).Start();

        var result1 = await kernel.SendAsync(new SubmitCode("GET https://example.com"));
        var traceparent1 = result1.Events.OfType<ReturnValueProduced>().Single().Value.As<HttpResponse>().Request.Headers["traceparent"];

        var result2 = await kernel.SendAsync(new SubmitCode("GET https://example.com"));
        var traceparent2 = result2.Events.OfType<ReturnValueProduced>().Single().Value.As<HttpResponse>().Request.Headers["traceparent"];

        // the traceparent format looks like this: 00-d00689882649007396cd32ab75c2611c-59402ab4fd5c55f7-00
        traceparent1.Single()[0..36].Should().NotBe(traceparent2.Single()[0..36]);
    }
}
