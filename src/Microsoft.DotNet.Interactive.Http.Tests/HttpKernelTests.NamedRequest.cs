// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Http.Tests.Utility;
using Microsoft.DotNet.Interactive.Tests.Utility;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Http.Tests
{
    [TestClass]
    public partial class HttpKernelTests
    {
        [TestClass]
        public class NamedRequest
        {
            [TestMethod]
            public async Task responses_can_be_accessed_as_symbols_in_later_requests()
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
                    response.Content = JsonContent.Create(builder.ToString());
                    return Task.FromResult(response);
                });

                var client = new HttpClient(largeResponseHandler);
                using var kernel = new HttpKernel("http", client, contentByteLengthThreshold: ContentByteLengthThreshold);

                var firstCode = """
                    @baseUrl = https://httpbin.org/anything

                    # @name login
                    POST {{baseUrl}}
                    Content-Type: application/json

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();

                var secondCode = """

                    @origin = {{login.response.body.$.origin}}
            
            
                    # @name createComment
                    POST https://example.com/api/comments HTTP/1.1
                    Content-Type: application/json
            
                    {
                        "origin" : {{origin}}
                    }
            
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                secondResult.Events.Should().NotContainErrors();
            }

            [TestMethod]
            public async Task response_headers_can_be_accessed_correctly()
            {
                var headerValue = string.Empty;
                var responseHandler = new InterceptingHttpMessageHandler((message, _) =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    if (message.Headers.TryGetValues("Detectedserver", out var values))
                    {
                        headerValue = values.First();
                    }

                    response.RequestMessage = message;
                    var contentString = """
                         {
                            "headers": {
                                "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7",
                                "Accept-Encoding": "gzip, deflate, br, zstd",
                                "Accept-Language": "en-US,en;q=0.9",
                                "Host": "httpbin.org",
                                "Priority": "u=0, i",
                                "Sec-Ch-Ua": "\""Chromium\"";v=\""128\"", \""Not;A=Brand\"";v=\""24\"", \""Microsoft Edge\"";v=\""128\"",
                                "Sec-Ch-Ua-Mobile": "?0",
                                "Sec-Ch-Ua-Platform": "\""Windows\"",
                                "Sec-Fetch-Dest": "document",
                                "Sec-Fetch-Mode": "navigate",
                                "Sec-Fetch-Site": "none",
                                "Sec-Fetch-User": "?1",
                                "Upgrade-Insecure-Requests": "1",
                                "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36 Edg/128.0.0.0",
                                "X-Amzn-Trace-Id": "Root=1-66e9f16e-7afea6f643e754f854c57d85"
                            }
                        }
                        """;
                    response.Content = new StringContent(contentString, Encoding.UTF8, "application/json");
                    response.Headers.Add("server", "gunicorn/19.9.0");
                    return Task.FromResult(response);
                });
                var client = new HttpClient(responseHandler);
                using var kernel = new HttpKernel("http", client);

                var firstCode = """
                    @baseUrl = https://httpbin.org/headers

                    # @name binHeader
                    GET {{baseUrl}}

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();


                var secondCode = """
                    GET https://httpbin.org/headers
                    Detectedserver: {{binHeader.response.headers.Server}}
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                secondResult.Events.Should().NotContainErrors();

                headerValue.Should().Be("gunicorn/19.9.0");
            }

            [TestMethod]
            public async Task response_headers_with_syntax_depth_of_five_can_be_accessed_correctly()
            {
                var headerValue = string.Empty;
                var responseHandler = new InterceptingHttpMessageHandler((message, _) =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    if (message.Headers.TryGetValues("X-Custom", out var values))
                    {
                        headerValue = values.First(n => n.Equals("theme=dark; Path=/; Expires=Wed, 09 Jun 2023 10:18:14 GMT"));
                    }

                    response.RequestMessage = message;
                    var contentString = """
                            {
                            "headers": {
                                "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7",
                                "Accept-Encoding": "gzip, deflate, br, zstd",
                                "Accept-Language": "en-US,en;q=0.9",
                                "Host": "httpbin.org",
                                "Priority": "u=0, i",
                                "Sec-Ch-Ua": "\""Chromium\"";v=\""128\"", \""Not;A=Brand\"";v=\""24\"", \""Microsoft Edge\"";v=\""128\"",
                                "Sec-Ch-Ua-Mobile": "?0",
                                "Sec-Ch-Ua-Platform": "\""Windows\"",
                                "Sec-Fetch-Dest": "document",
                                "Sec-Fetch-Mode": "navigate",
                                "Sec-Fetch-Site": "none",
                                "Sec-Fetch-User": "?1",
                                "Upgrade-Insecure-Requests": "1",
                                "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36 Edg/128.0.0.0",
                                "X-Amzn-Trace-Id": "Root=1-66e9f16e-7afea6f643e754f854c57d85"
                            }
                        }
                        """;
                    response.Content = new StringContent(contentString, Encoding.UTF8, "application/json");
                    response.Headers.Add("Set-Cookie", "sessionId=abc123; Path=/; HttpOnly");
                    response.Headers.Add("Set-Cookie", "userId=789xyz; Path=/; Secure");
                    response.Headers.Add("Set-Cookie", "theme=dark; Path=/; Expires=Wed, 09 Jun 2023 10:18:14 GMT");

                    return Task.FromResult(response);
                });
                var client = new HttpClient(responseHandler);
                using var kernel = new HttpKernel("http", client);

                var firstCode = """
                    @baseUrl = https://httpbin.org/headers

                    # @name binHeader
                    GET {{baseUrl}}

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();


                var secondCode = """
                    GET https://httpbin.org/headers
                    X-Custom: {{binHeader.response.headers.Set-Cookie.theme=dark; Path=/; Expires=Wed, 09 Jun 2023 10:18:14 GMT}}
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                secondResult.Events.Should().NotContainErrors();

                headerValue.Should().Be("theme=dark; Path=/; Expires=Wed, 09 Jun 2023 10:18:14 GMT");
            }

            [TestMethod]
            public async Task attempting_to_access_response_headers_with_excessive_syntax_depth_will_produce_an_error()
            {
                var headerValue = string.Empty;
                var responseHandler = new InterceptingHttpMessageHandler((message, _) =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    if (message.Headers.TryGetValues("Detectedserver", out var values))
                    {
                        headerValue = values.First();
                    }

                    response.RequestMessage = message;
                    var contentString = """
                         {
                            "headers": {
                                "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7",
                                "Accept-Encoding": "gzip, deflate, br, zstd",
                                "Accept-Language": "en-US,en;q=0.9",
                                "Host": "httpbin.org",
                                "Priority": "u=0, i",
                                "Sec-Ch-Ua": "\""Chromium\"";v=\""128\"", \""Not;A=Brand\"";v=\""24\"", \""Microsoft Edge\"";v=\""128\"",
                                "Sec-Ch-Ua-Mobile": "?0",
                                "Sec-Ch-Ua-Platform": "\""Windows\"",
                                "Sec-Fetch-Dest": "document",
                                "Sec-Fetch-Mode": "navigate",
                                "Sec-Fetch-Site": "none",
                                "Sec-Fetch-User": "?1",
                                "Upgrade-Insecure-Requests": "1",
                                "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36 Edg/128.0.0.0",
                                "X-Amzn-Trace-Id": "Root=1-66e9f16e-7afea6f643e754f854c57d85"
                            }
                        }
                        """;
                    response.Content = new StringContent(contentString, Encoding.UTF8, "application/json");
                    response.Headers.Add("server", "gunicorn/19.9.0");
                    return Task.FromResult(response);
                });
                var client = new HttpClient(responseHandler);
                using var kernel = new HttpKernel("http", client);

                var firstCode = """
                    @baseUrl = https://httpbin.org/headers

                    # @name binHeader
                    GET {{baseUrl}}

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();


                var secondCode = """
                    GET https://httpbin.org/headers
                    Detectedserver: {{binHeader.response.headers.Server.gunicorn}}
                    ###
                    """
                ;
                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));
                var diagnostics = secondResult.Events.Should().ContainSingle<DiagnosticsProduced>().Which;
                diagnostics.Diagnostics.First().Message.Should().Be($$$"""The supplied expression 'binHeader.response.headers.Server.gunicorn' does not follow the correct pattern. The expression should adhere to the following pattern: {{requestName.(response|request).(body|headers).(*|JSONPath|XPath|Header Name)}}.""");
            }

            [TestMethod]
            public async Task accessing_a_response_header_that_does_not_exist_will_produce_an_error()
            {
                var headerValue = string.Empty;
                var responseHandler = new InterceptingHttpMessageHandler((message, _) =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    if (message.Headers.TryGetValues("Detectedserver", out var values))
                    {
                        headerValue = values.First();
                    }

                    response.RequestMessage = message;
                    var contentString = """
                         {
                            "headers": {
                                "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7",
                                "Accept-Encoding": "gzip, deflate, br, zstd",
                                "Accept-Language": "en-US,en;q=0.9",
                                "Host": "httpbin.org",
                                "Priority": "u=0, i",
                                "Sec-Ch-Ua": "\""Chromium\"";v=\""128\"", \""Not;A=Brand\"";v=\""24\"", \""Microsoft Edge\"";v=\""128\"",
                                "Sec-Ch-Ua-Mobile": "?0",
                                "Sec-Ch-Ua-Platform": "\""Windows\"",
                                "Sec-Fetch-Dest": "document",
                                "Sec-Fetch-Mode": "navigate",
                                "Sec-Fetch-Site": "none",
                                "Sec-Fetch-User": "?1",
                                "Upgrade-Insecure-Requests": "1",
                                "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36 Edg/128.0.0.0",
                                "X-Amzn-Trace-Id": "Root=1-66e9f16e-7afea6f643e754f854c57d85"
                            }
                        }
                        """;
                    response.Content = new StringContent(contentString, Encoding.UTF8, "application/json");
                    response.Headers.Add("server", "gunicorn/19.9.0");
                    return Task.FromResult(response);
                });
                var client = new HttpClient(responseHandler);
                using var kernel = new HttpKernel("http", client);

                var firstCode = """
                    @baseUrl = https://httpbin.org/headers

                    # @name binHeader
                    GET {{baseUrl}}

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();


                var secondCode = """
                    GET https://httpbin.org/headers
                    Detectedserver: {{binHeader.response.headers.Accept}}
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));
                var diagnostics = secondResult.Events.Should().ContainSingle<DiagnosticsProduced>().Which;
                diagnostics.Diagnostics.First().Message.Should().Be($$$"""The supplied header name 'Accept' does not exist in the named request.""");
            }

            [TestMethod]
            [DataRow("json.response.body.$.slideshow.slides.title", "Wake up to WonderWidgets!")]
            [DataRow("json.response.body.$.slideshow.slides.type", "all")]
            public async Task json_with_additional_syntax_depths_can_be_accessed_correctly(string path, string expectedValue)
            {

                var responseHandler = new InterceptingHttpMessageHandler((message, _) =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.RequestMessage = message;
                    var contentString = """
                                {
                      "slideshow": {
                        "author": "Yours Truly",
                        "date": "date of publication",
                        "slides": [
                          {
                            "title": "Wake up to WonderWidgets!",
                            "type": "all"
                          },
                          {
                            "items": [
                              "Why <em>WonderWidgets</em> are great",
                              "Who <em>buys</em> WonderWidgets"
                            ],
                            "title": "Overview",
                            "type": "all"
                          }
                        ],
                        "title": "Sample Slide Show"
                      }
                    }
                    """;
                    response.Content = new StringContent(contentString, Encoding.UTF8, "application/json");
                    return Task.FromResult(response);
                });
                var client = new HttpClient(responseHandler);
                using var kernel = new HttpKernel("http", client);

                var firstCode = """
                    @baseUrl = https://httpbin.org/json

                    # @name json
                    GET {{baseUrl}}
                    Content-Type: application/json

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();

                var secondCode = $$$"""
                    @pathContents = {{{{{path}}}}}
                    
                    
                    # @name createComment
                    POST https://example.com/api/comments HTTP/1.1
                    Content-Type: application/json
                    
                    {
                        "path" :{{pathContents}}
                    }
                    
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                secondResult.Events.Should().NotContainErrors();

                var returnValue = secondResult.Events.OfType<ReturnValueProduced>().First();

                var response = (HttpResponse)returnValue.Value;

                response.Request.Content.Raw.Split(":").Last().TrimEnd("\r\n}".ToCharArray()).Should().Be(expectedValue);

            }

            [TestMethod]
            [DataRow("login.response.$")]
            [DataRow("login.response.//")]
            [DataRow("login.request.$")]
            [DataRow("login.request.//")]
            public async Task responses_with_incomplete_paths_produces_errors(string path)
            {

                using var kernel = new HttpKernel();

                var firstCode = """
                    @baseUrl = https://httpbin.org/anything

                    # @name login
                    POST {{baseUrl}}
                    Content-Type: application/json

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();

                var secondCode = $$$"""
                    @origin = {{{{{path}}}}}
                    
                    
                    # @name createComment
                    POST https://example.com/api/comments HTTP/1.1
                    Content-Type: application/json
                    
                    {
                        "origin" : {{origin}}
                    }
                    
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                var diagnostics = secondResult.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

                diagnostics.Diagnostics.First().Message.Should().Be($$$"""The supplied expression '{{{path}}}' does not follow the correct pattern. The expression should adhere to the following pattern: {{requestName.(response|request).(body|headers).(*|JSONPath|XPath|Header Name)}}.""");
            }


            [TestMethod]
            [DataRow("login.request.body.$.test", "application/json")]
            [DataRow("login.request.body.//test", "application/xml")]
            public async Task incomplete_syntax_depths_produces_errors(string path, string contentType)
            {
                using var kernel = new HttpKernel();

                var firstCode = $$$"""
                    @baseUrl = https://httpbin.org/anything

                    # @name login
                    POST {{baseUrl}}
                    Content-Type: {{{contentType}}}

                    {
                        "test": testing
                    }

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();

                var secondCode = $$$"""
                    @origin = {{{{{path}}}}}
                    
                    
                    # @name createComment
                    POST https://example.com/api/comments HTTP/1.1
                    Content-Type: application/json
                    
                    {
                        "origin" : {{origin}}
                    }
                    
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                var diagnostics = secondResult.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

                diagnostics.Diagnostics.First().Message.Should().Be($"The named request does not contain any content at this path '{path}'.");
            }

            [TestMethod]
            public async Task json_with_xml_content_type_produces_errors()
            {
                using var kernel = GetHttpKernelWithMockedResponses(
                    ("<test>testing!</test>", "application/xml"));

                var firstCode = """
                    @baseUrl = https://httpbin.org/xml

                    # @name login
                    GET {{baseUrl}}

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();

                var secondCode = """
                    @origin = {{login.response.body.$.test}}
                    
                    POST https://example.com/api/comments HTTP/1.1
                    Content-Type: application/json
                    
                    {
                        "origin" : {{origin}}
                    }
                    
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                var diagnostics = secondResult.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

                diagnostics.Diagnostics.First().Message.Should().Be("""The supplied named request has content type of 'application/xml' which differs from the required content type of 'application/json'.""");
            }

            [TestMethod]
            public async Task xml_with_json_content_type_produces_errors()
            {
                using var kernel = GetHttpKernelWithMockedResponses(("{}", "application/json"));

                var firstCode = """
                    @baseUrl = https://httpbin.org/anything

                    # @name login
                    POST {{baseUrl}}
                    Content-Type: application/json

                    {
                        "test": testing
                    }

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();

                var secondCode = """
                    @origin = {{login.response.body.//test}}
                    
                    
                    # @name createComment
                    POST https://example.com/api/comments HTTP/1.1
                    Content-Type: application/json
                    
                    {
                        "origin" : {{origin}}
                    }
                    
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                var diagnostics = secondResult.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

                diagnostics.Diagnostics.First().Message.Should().Be("The supplied named request has content type of 'application/json' which differs from the required content type of 'application/xml'.");
            }

            [TestMethod]
            [DataRow("login.request.body.$")]
            [DataRow("login.request.body.//")]
            public async Task no_body_produces_errors_when_trying_to_access(string path)
            {
                using var kernel = new HttpKernel();

                var firstCode = """
                    @baseUrl = https://httpbin.org/anything

                    # @name login
                    POST {{baseUrl}}

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();

                var secondCode = $$$"""
                    @origin = {{{{{path}}}}}
                    
                    
                    # @name createComment
                    POST https://example.com/api/comments HTTP/1.1
                    Content-Type: application/json
                    
                    {
                        "origin" : {{origin}}
                    }
                    
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                var diagnostics = secondResult.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

                diagnostics.Diagnostics.First().Message.Should().Be("""The supplied named request 'login' does not have a request body.""");
            }

            [TestMethod]
            public async Task responses_can_be_accessed_as_xml_in_later_requests()
            {
                using var kernel = GetHttpKernelWithMockedResponses(("""
                                                                     <?xml version='1.0' encoding='us-ascii'?> <!-- A SAMPLE set of slides --> <slideshow title="Sample Slide Show" date="Date of publication" author="Yours Truly" > <!-- TITLE SLIDE --> <slide type="all"> <title>Wake up to WonderWidgets!</title> </slide> <!-- OVERVIEW --> <slide type="all"> <title>Overview</title> <item>Why <em>WonderWidgets</em> are great</item> <item/> <item>Who <em>buys</em> WonderWidgets</item> </slide> </slideshow>
                                                                     """, "application/xml"),
                    ("", "application/xml"));

                using var _ = new AssertionScope();

                var firstCode = """
                    @baseUrl = https://httpbin.org/xml

                    # @name sampleXml
                    GET {{baseUrl}}
                    Content-Type: application/xml

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();

                var secondCode = """
                    POST https://example.com/api/comments HTTP/1.1
                    X-ValFromPrevious: {{sampleXml.response.body.//slideshow/slide[2]/title}}
                    
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                secondResult.Events.Should().NotContainErrors();
            }

            [TestMethod]
            [DataRow("example.request.body.*")]
            [DataRow("example.response.body.*")]
            public async Task body_content_produces_the_entirety_of_the_body_content(string path)
            {
                using var kernel = new HttpKernel();

                var firstCode = """
                    @baseUrl = https://httpbin.org/anything

                    # @name example
                    POST {{baseUrl}}
                    Accept: application/json

                    {
                        "sample" : "text"
                    }
                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();

                var secondCode = $$$"""            
                    # @name createComment
                    POST https://example.com/api/comments HTTP/1.1
                    {{{{{path}}}}}
                    
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                secondResult.Events.Should().NotContainErrors();
            }

            [TestMethod]
            public async Task improper_xml_path_produces_errors()
            {
                using var kernel = GetHttpKernelWithMockedResponses(("<test>testing!</test>", "application/xml"));

                using var _ = new AssertionScope();

                var firstCode = """
                    @baseUrl = https://httpbin.org/xml

                    # @name sampleXml
                    GET {{baseUrl}}
                    Content-Type: application/xml

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();

                var secondCode = """

                    POST https://example.com/api/comments HTTP/1.1
                    X-ValFromPrevious: {{sampleXml.response.body.//slideshow/slide[2]/title}}
            
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                var diagnostics = secondResult.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

                diagnostics.Diagnostics.First().Message.Should().Be("""
                                                                    The supplied XML path '//slideshow/slide[2]/title' does not exist in the named request.
                                                                    """);
            }

            [TestMethod]
            public async Task responses_can_be_accessed_through_headers_in_later_requests()
            {
                using var kernel = new HttpKernel();

                using var _ = new AssertionScope();

                var firstCode = """
                    @baseUrl = https://httpbin.org

                    # @name sample
                    GET {{baseUrl}}
                    Content-Type: application/json

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();

                var secondCode = """

                    POST https://example.com/api/comments HTTP/1.1
                    Server: {{sample.response.headers.Server}}
            
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                secondResult.Events.Should().NotContainErrors();
            }


            [TestMethod]
            public async Task invalid_named_request_property_produces_errors()
            {
                using var kernel = new HttpKernel();

                using var _ = new AssertionScope();

                var firstCode = """
                    @baseUrl = https://example.com/api

                    # @name login
                    POST {{baseUrl}}/api/login HTTP/1.1
                    Content-Type: application/x-www-form-urlencoded

                    name=foo&password=bar

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();

                var secondCode = """

                    @authToken = {{login.response.headers.X-AuthToken}}
            
            
                    # @name createComment
                    POST https://example.com/api/comments HTTP/1.1
                    Authorization: {{authToken}}
                    Content-Type: application/json
            
                    {
                        "content": "fake content"
                    }
            
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                secondResult.Events.Count.Should().Be(2);
            }

            [TestMethod]
            [DataRow("example.request.headers.*")]
            [DataRow("example.response.headers.*")]
            public async Task Accessing_content_body_after_headers_is_not_supported_and_produces_errors(string path)
            {
                using var kernel = new HttpKernel();

                var firstCode = """
                    @baseUrl = https://httpbin.org/anything

                    # @name example
                    POST {{baseUrl}}
                    Accept: application/json

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();

                var secondCode = $$$"""            
                    # @name createComment
                    POST https://example.com/api/comments HTTP/1.1
                    {{{{{path}}}}}
                    
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                var diagnostics = secondResult.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

                diagnostics.Diagnostics.First().Message.Should().Be($$$"""The supplied header name '*' does not exist in the named request.""");
            }

            [TestMethod]
            [DataRow("example.request.headers.Content-Type", "Content-Type")]
            [DataRow("example.response.headers.Authorization", "Authorization")]
            public async Task accessing_non_existent_header_names_produces_an_error(string path, string headerName)
            {
                using var kernel = new HttpKernel();

                var firstCode = """
                    @baseUrl = https://httpbin.org/anything

                    # @name example
                    POST {{baseUrl}}
                    Accept: application/json

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();

                var secondCode = $$$"""
                    @headerName = {{{{{path}}}}}
                    
                    
                    # @name createComment
                    POST https://example.com/api/comments HTTP/1.1
                    Content-Type: application/json
                    
                    {
                        "headerName" : {{headerName}}
                    }
                    
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                var diagnostics = secondResult.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

                diagnostics.Diagnostics.First().Message.Should().Be($$$"""The supplied header name '{{{headerName}}}' does not exist in the named request.""");
            }

            [TestMethod]
            public async Task accessing_an_index_in_a_json_array_succeeds()
            {
                using var kernel = new HttpKernel();
                var firstRequest = """                    
                    # @name sampleArray
                    POST https://httpbin.org/anything
                    {
                      "devices": [
                        {
                          "id": "5601db0f-32e0-4d82-bc79-251e50fa1407",
                          "name": "Foo"
                        },
                        {
                          "id": "455301a5-8a6e-49d0-b056-96fb2847be18",
                          "name": "Bar"
                        }
                      ]
                    }
                    ###
                    """;
                var firstResult = await kernel.SendAsync(new SubmitCode(firstRequest));
                firstResult.Events.Should().NotContainErrors();
                var secondRequest = """
                    GET https://httpbin.org/headers
                    X-Value: {{sampleArray.response.body.$.json.devices[0].id}}
                    ###
                    """;
                var secondResult = await kernel.SendAsync(new SubmitCode(secondRequest));
                secondResult.Events.Should().NotContainErrors();

                var returnValue = secondResult.Events.OfType<ReturnValueProduced>().First();

                var response = (HttpResponse)returnValue.Value;

                response.Request.Headers["X-Value"].First().Should().Be("5601db0f-32e0-4d82-bc79-251e50fa1407");
            }

            [TestMethod]
            public async Task accessing_multiple_indexes_in_a_json_array_succeeds()
            {
                using var kernel = new HttpKernel();
                var firstRequest = """                    
                    # @name sampleArray
                    POST https://httpbin.org/anything
                    {
                      "devices": [
                        {
                          "id": "5601db0f-32e0-4d82-bc79-251e50fa1407",
                          "name": "Foo"
                        },
                        {
                          "ids": ["455301a5-8a6e-49d0-b056-96fb2847be18", "455301a5-8a6e-49d0-b056-96fb2847be19", "455301a5-8a6e-49d0-b056-96fb2847be20"], 
                          "name": "Bar"
                        }
                      ]
                    }
                    ###
                    """;
                var firstResult = await kernel.SendAsync(new SubmitCode(firstRequest));
                firstResult.Events.Should().NotContainErrors();
                var secondRequest = """
                    GET https://httpbin.org/headers
                    X-Value: {{sampleArray.response.body.$.json.devices[1].ids[2]}}
                    ###
                    """;
                var secondResult = await kernel.SendAsync(new SubmitCode(secondRequest));
                secondResult.Events.Should().NotContainErrors();

                var returnValue = secondResult.Events.OfType<ReturnValueProduced>().First();

                var response = (HttpResponse)returnValue.Value;

                response.Request.Headers["X-Value"].First().Should().Be("455301a5-8a6e-49d0-b056-96fb2847be20");
            }

            [TestMethod]
            public async Task attempting_to_access_headers_that_do_not_exist_will_produce_an_error()
            {
                using var kernel = new HttpKernel();

                var firstCode = """
                    @baseUrl = https://httpbin.org/anything

                    # @name example
                    POST {{baseUrl}}

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();

                var secondCode = """
                    @headerName = {{example.request.headers.Content-Type}}
                    
                    
                    # @name createComment
                    POST https://example.com/api/comments HTTP/1.1
                    Content-Type: application/json
                    
                    {
                        "headerName" : {{headerName}}
                    }
                    
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                var diagnostics = secondResult.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

                diagnostics.Diagnostics.First().Message.Should().Be("The supplied named request 'example' does not have any headers.");
            }

            [TestMethod]
            public async Task variables_have_precedence_over_named_requests()
            {
                using var kernel = new HttpKernel();

                var firstCode = """
                    @baseUrl = https://httpbin.org/anything

                    # @name example
                    POST {{baseUrl}}

                    ###
                    """;

                var firstResult = await kernel.SendAsync(new SubmitCode(firstCode));
                firstResult.Events.Should().NotContainErrors();

                var secondCode = """
                    @example.response.headers.Accept = application/xml
                    @headerName = {{example.response.headers.Accept}}
                   
                    # @name createComment
                    POST https://example.com/api/comments HTTP/1.1
                    Accept: {{example.response.headers.Accept}}
                    
                    {
                        "headerName" : {{example.response.headers.Accept}}
                    }
                    
                    ###
                    """;

                var secondResult = await kernel.SendAsync(new SubmitCode(secondCode));

                secondResult.Events.Should().NotContainErrors();
            }

            [TestMethod]
            public async Task attempting_to_use_named_request_prior_to_run_will_cause_failure()
            {
                using var kernel = new HttpKernel();

                var firstCode = """
                    @baseUrl = https://httpbin.org/anything
                    
                    # @name example
                    POST {{baseUrl}}
                    
                    ###
                   
                    # @name createComment
                    POST https://example.com/api/comments HTTP/1.1
                    
                    
                    {
                        "Server" : {{example.response.headers.Server}}
                    }
                    
                    ###
                    """;

                var result = await kernel.SendAsync(new SubmitCode(firstCode));

                var diagnostics = result.Events.Should().ContainSingle<DiagnosticsProduced>().Which;

                diagnostics.Diagnostics.First().Message.Should().Be("Unable to evaluate expression 'example.response.headers.Server'.");
            }

            private static HttpKernel GetHttpKernelWithMockedResponses(
                params (string content, string contentType)[] responses)
            {
                HttpKernel kernel = null;
                var funcs = new Queue<(string content, string contentType)>(responses);
                var handler = new InterceptingHttpMessageHandler((_, _) =>
                {
                    var t = funcs.Dequeue();
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(t.content, new MediaTypeHeaderValue(t.contentType))
                    };
                    return Task.FromResult(response);
                });
                var client = new HttpClient(handler);
                kernel = new HttpKernel(client: client);
                return kernel;
            }
        }
    }
}
