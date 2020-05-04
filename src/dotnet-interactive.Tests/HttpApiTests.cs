// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.App.Lsp;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Extensions;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.LanguageService;
using Microsoft.DotNet.Interactive.Server;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pocket;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class HttpApiTests : IDisposable
    {
        private readonly Dictionary<Language, InProcessTestServer> _servers = new Dictionary<Language, InProcessTestServer>();

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public void Dispose()
        {
            _disposables.Add(Formatting.Formatter.ResetToDefault);
            _disposables.Dispose();
        }

        private InProcessTestServer GetServer()
        {
            return GetServer(Language.CSharp);
        }

        private InProcessTestServer GetServer(Language language)
        {
            if (_servers.TryGetValue(language, out var testServer))
            {
                return testServer;
            }

            var newServer = InProcessTestServer.StartServer($"http --default-kernel {language.LanguageName()} --http-port 4242");
            _servers.Add(language, newServer);
            return newServer;
        }

        [Fact]
        public async Task FrontendEnvironment_host_is_set_via_handshake()
        {
            var expectedUri = new Uri("http://choosen.one:1000/");
            var response = await GetServer().HttpClient.PostAsync("/discovery", new StringContent(expectedUri.AbsoluteUri));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            GetServer().FrontendEnvironment.ApiUri.Should().Be(expectedUri);
        }

        [Theory]
        [InlineData(Language.CSharp, "var a = 123;")]
        [InlineData(Language.FSharp, "let a = 123")]
        public async Task can_get_variable_value(Language language, string code)
        {
            await GetServer(language).Kernel.SendAsync(new SubmitCode(code, language.LanguageName()));

            var response = await GetServer(language).HttpClient.GetAsync($"/variables/{language.LanguageName()}/a");

            await response.ShouldSucceed();

            var responseContent = await response.Content.ReadAsStringAsync();

            responseContent.Should().BeJsonEquivalentTo(123);
        }

        [Theory]
        [InlineData(Language.CSharp, "var a = \"Code value\";")]
        [InlineData(Language.FSharp, "let a = \"Code value\"")]
        public async Task can_get_variable_value_whn_variable_is_string(Language language, string code)
        {
            await GetServer(language).Kernel.SendAsync(new SubmitCode(code, language.LanguageName()));

            var response = await GetServer(language).HttpClient.GetAsync($"/variables/{language.LanguageName()}/a");

            var responseContent = await response.Content.ReadAsStringAsync();
            
            responseContent.Should().BeJsonEquivalentTo("Code value");
        }

        [Fact]
        public async Task can_get_variables_with_bulk_request()
        {
            await GetServer().Kernel.SendAsync(new SubmitCode(@"
var a = 123;
var b = ""1/2/3"";
var f = new { Field= ""string value""};", Language.CSharp.LanguageName()));

            await GetServer().Kernel.SendAsync(new SubmitCode(@"
let d = 567", Language.FSharp.LanguageName()));


            var request = new
            {
                csharp = new[] { "a", "f", "b" },
                fsharp = new[] { "d" }
            };

            var response = await GetServer().HttpClient.PostAsync("/variables/",new StringContent(JsonConvert.SerializeObject( request)));

            var responseContent = await response.Content.ReadAsStringAsync();

            var expected = new
            {
                csharp = new
                {
                    a = 123,
                    b = "1/2/3",
                    f = new { Field = "string value" }
                },
                fsharp = new
                {
                    d = 567
                }
            };

            responseContent.Should().BeJsonEquivalentTo(expected);
        }

        [Fact]
        public async Task bulk_variable_request_is_returned_with_application_json_content_type()
        {
            await GetServer().Kernel.SendAsync(new SubmitCode(@"
var a = 123;
var b = ""1/2/3"";
var f = new { Field= ""string value""};", Language.CSharp.LanguageName()));


            var request = new
            {
                csharp = new[] { "a", "f", "b" }

            };

            var response = await GetServer().HttpClient.PostAsync("/variables/", new StringContent(JsonConvert.SerializeObject(request)));

            response.Content.Headers.ContentType.MediaType.Should().Be("application/json");
        }

        [Fact]
        public async Task Variable_serialization_can_be_customized_using_Formatter()
        {
            Formatter<FileInfo>.Register(
                info => new { TheName = info.Name }.SerializeToJson().Value,
                JsonFormatter.MimeType);

            await GetServer().Kernel.SendAsync(new SubmitCode("var theFile = new System.IO.FileInfo(\"the-file.txt\");"));

            var response = await GetServer().HttpClient.GetAsync("/variables/csharp/theFile");

            var responseContent = await response.Content.ReadAsStringAsync();

            var value = JObject.Parse(responseContent);

            value["TheName"].Value<string>().Should().Be("the-file.txt");
        }

        [Theory]
        [InlineData(Language.CSharp, "var a = 123;")]
        [InlineData(Language.FSharp, "let a = 123")]
        public async Task variable_is_returned_with_application_json_content_type(Language language, string code)
        {
            await GetServer(language).Kernel.SendAsync(new SubmitCode(code, language.LanguageName()));

            var response = await GetServer(language).HttpClient.GetAsync($"/variables/{language.LanguageName()}/a");

            await response.ShouldSucceed();

            response.Content.Headers.ContentType.MediaType.Should().Be("application/json");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task When_variable_does_not_exist_then_it_returns_404(Language language)
        {
            var response = await GetServer(language).HttpClient.GetAsync($"/variables/{language.LanguageName()}/does_not_exist");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task When_subkernel_does_not_exist_then_it_returns_404()
        {
            var response = await GetServer().HttpClient.GetAsync("/variables/does_not_exist/does_not_exist");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Theory]
        [InlineData(Language.CSharp, "var x = 1234;", 0, 10, "readonly struct System.Int32")]
        public async Task lsp_textDocument_hover_returns_expected_result(Language language, string code, int line, int character, string expected)
        {
            using var _ = new AssertionScope();
            var request = new HoverParams(
                TextDocument.FromDocumentContents(code),
                new Lsp.Position(line, character));
            var response = await GetServer(language).HttpClient.PostObjectAsJsonAsync("/lsp/textDocument/hover", request);
            await response.ShouldSucceed();
            var responseJson = await response.Content.ReadAsStringAsync();
            var hoverResponse = LspDeserializeFromString<HoverResponse>(responseJson);
            hoverResponse.Contents.Kind.Should().Be(Lsp.MarkupKind.Markdown);
            hoverResponse.Contents.Value.Should().Be(expected);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        public async Task lsp_textDocument_hover_returns_400_on_malformed_request_object(Language language)
        {
            using var _ = new AssertionScope();
            var request = new
            {
                SomeField = 1,
                SomeOtherField = "test",
            };
            var response = await GetServer(language).HttpClient.PostObjectAsJsonAsync("/lsp/textDocument/hover", request);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().StartWith("unable to parse request object");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        [InlineData(Language.PowerShell)]
        public async Task unsupported_lsp_methods_return_404(Language language)
        {
            // something that's not any LSP method
            var response = await GetServer(language).HttpClient.PostObjectAsJsonAsync($"/lsp/not/a/method", new object()); // payload doesn't matter
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Theory]
        [InlineData(Language.FSharp, "let x = 12$$34")]
        [InlineData(Language.PowerShell, "$x = 12$$34")]
        public async Task unimplemented_lsp_handlers_return_404(Language language, string markupCode)
        {
            // something that's a valid LSP method, just not for the given kernel
            var methodName = "textDocument/hover";
            MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);
            var request = new HoverParams(
                TextDocument.FromDocumentContents(code),
                new Lsp.Position(line, character));
            var response = await GetServer(language).HttpClient.PostObjectAsJsonAsync($"/lsp/{methodName}", request);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().StartWith($"method '{methodName}' not found on kernel '{language.LanguageName()}'");
        }

        [Fact]
        public async Task can_get_static_content()
        {
            var response = await GetServer().HttpClient.GetAsync("/resources/logo-32x32.png");

            await response.ShouldSucceed();

            response.Content.Headers.ContentType.MediaType.Should().Be("image/png");
        }

        [Fact]
        public async Task can_get_kernel_names()
        {
            var response = await GetServer().HttpClient.GetAsync("/kernels");

            await response.ShouldSucceed();

            response.Content.Headers.ContentType.MediaType.Should().Be("application/json");

            var responseContent = await response.Content.ReadAsStringAsync();
            var kernels = JToken.Parse(responseContent).Values<string>();

            kernels.Should()
                   .BeEquivalentTo(
                       ".NET", 
                       "csharp", 
                       "fsharp", 
                       "powershell",
                       "html",
                       "javascript");
        }

        public static T LspDeserializeFromString<T>(string text)
        {
            var reader = new StringReader(text);
            var result = (T)LspSerializer.JsonSerializer.Deserialize(reader, typeof(T));
            return result;
        }
    }

    internal static class HttpClientTestExtensions
    {
        public static async Task<HttpResponseMessage> PostObjectAsJsonAsync(this HttpClient client, string requestUri, object request)
        {
            var requestJObject = JObject.FromObject(request, LspSerializer.JsonSerializer);
            var content = new StringContent(requestJObject.ToString(), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(requestUri, content);
            return response;
        }

        public static async Task ShouldSucceed(this HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                // this block is wrapped so `response.Content` isn't prematurely consumed
                var content = await response.Content.ReadAsStringAsync();
                response.IsSuccessStatusCode
                    .Should()
                    .BeTrue($"Response status code indicates failure: {(int)response.StatusCode} ({response.StatusCode}):\n{content}");
            }
        }
    }
}
