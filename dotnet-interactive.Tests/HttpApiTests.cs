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
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.LanguageService;
using Microsoft.DotNet.Interactive.Tests;
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

            var newServer = InProcessTestServer.StartServer($"http --default-kernel {language.LanguageName()}");
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
        [InlineData(Language.CSharp, "Console.WriteLine()", 0, 10)]
        public async Task lsp_textDocument_hover_returns_expected_content(Language language, string code, int line, int column)
        {
            using var _ = new AssertionScope();
            var hoverParams = new HoverParams()
            {
                TextDocument = TextDocument.FromDocumentContents(code),
                Position = new Position(line, column),
            };
            var request = JObject.FromObject(hoverParams);
            var response = await GetServer(language).HttpClient.PostJsonAsync("/lsp/textDocument/hover", request);
            await response.ShouldSucceed();
            var responseJson = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(responseJson);
            var hoverResponse = json.ToLspObject<TextDocumentHoverResponse>();
            hoverResponse.Contents.Kind.Should().Be(MarkupKind.Markdown);
            hoverResponse.Contents.Value.Should().Match("void Console.WriteLine() (+ * overloads)");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        public async Task unimplemented_lsp_methods_return_empty_string(Language language)
        {
            // language kernels that implement LSP handling, but not for the specified method
            var response = await GetServer(language).HttpClient.PostJsonAsync($"/lsp/not/a/method", new JObject());
            await response.ShouldSucceed();
            var responseJson = await response.Content.ReadAsStringAsync();
            responseJson.Should().BeEmpty();
        }

        [Theory]
        [InlineData(Language.FSharp)]
        [InlineData(Language.PowerShell)]
        public async Task unimplemented_lsp_handlers_return_empty_string(Language language)
        {
            // language kernels that don't implement any LSP handling
            var response = await GetServer(language).HttpClient.PostJsonAsync($"/lsp/textDocument/hover", new JObject());
            await response.ShouldSucceed();
            var responseJson = await response.Content.ReadAsStringAsync();
            responseJson.Should().BeEmpty();
        }

        [Fact]
        public async Task lsp_methods_run_deferred_commands()
        {
            // declare a variable in deferred code
            var kernelBase = GetServer().Kernel as KernelBase;
            kernelBase.DeferCommand(new SubmitCode("var one = 1;"));

            // it's not defined
            var response = await GetServer().HttpClient.GetAsync($"/variables/{Language.CSharp.LanguageName()}/one");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            // ensure we can get http info about it
            var code = "Console.WriteLine(one);";
            //                             ^ (0, 20)
            var request = new HoverParams()
            {
                TextDocument = TextDocument.FromDocumentContents(code),
                Position = new Position()
                {
                    Line = 0,
                    Character = 20,
                },
            };
            response = await GetServer().HttpClient.PostJsonAsync("/lsp/textDocument/hover", request.SerializeLspObject());
            await response.ShouldSucceed();
            var responseJson = await response.Content.ReadAsStringAsync();
            var hover = JsonConvert.DeserializeObject<TextDocumentHoverResponse>(responseJson);
            hover.Contents.Value.Should().Be("(field) int one");

            // and it now exists
            response = await GetServer().HttpClient.GetAsync($"/variables/{Language.CSharp.LanguageName()}/one");
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().BeJsonEquivalentTo(1);
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

        [Fact]
        public async Task can_get_javascript_api()
        {
            var response = await GetServer().HttpClient.GetAsync("/resources/dotnet-interactive.js");

            await response.ShouldSucceed();

            response.Content.Headers.ContentType.MediaType.Should().Be("application/javascript");
        }
    }

    internal static class HttpClientTestExtensions
    {
        public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string requestUri, JObject requestBody)
        {
            return client.PostJsonAsync(requestUri, requestBody.ToString());
        }

        public static async Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string requestUri, string requestBody)
        {
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
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
