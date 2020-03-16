// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Tests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pocket;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class HttpApiTests : IDisposable
    {
        private readonly InProcessTestServer _server;

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public HttpApiTests()
        {
            _server = InProcessTestServer.StartServer("http --default-kernel csharp");

            _disposables.Add(Formatting.Formatter.ResetToDefault);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        [Fact]
        public async Task FrontendEnvironment_host_is_set_via_handshake()
        {
            var expectedUri = new Uri("http://choosen.one:1000/");
            var response = await _server.HttpClient.PostAsync("/discovery", new StringContent(expectedUri.AbsoluteUri));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _server.FrontendEnvironment.ApiUri.Should().Be(expectedUri);
        }

        [Theory]
        [InlineData(Language.CSharp, "var a = 123;")]
        [InlineData(Language.FSharp, "let a = 123")]
        public async Task can_get_variable_value(Language language, string code)
        {
            await _server.Kernel.SendAsync(new SubmitCode(code, language.LanguageName()));

            var response = await _server.HttpClient.GetAsync($"/variables/{language.LanguageName()}/a");

            var responseContent = await response.Content.ReadAsStringAsync();

            responseContent.Should().BeJsonEquivalentTo(123);
        }

        [Theory]
        [InlineData(Language.CSharp, "var a = \"Code value\";")]
        [InlineData(Language.FSharp, "let a = \"Code value\"")]
        public async Task can_get_variable_value_whn_variable_is_string(Language language, string code)
        {
            await _server.Kernel.SendAsync(new SubmitCode(code, language.LanguageName()));

            var response = await _server.HttpClient.GetAsync($"/variables/{language.LanguageName()}/a");

            var responseContent = await response.Content.ReadAsStringAsync();
            
            responseContent.Should().BeJsonEquivalentTo("Code value");
        }

        [Fact]
        public async Task can_get_variables_with_bulk_request()
        {
            await _server.Kernel.SendAsync(new SubmitCode(@"
var a = 123;
var b = ""1/2/3"";
var f = new { Field= ""string value""};", Language.CSharp.LanguageName()));

            await _server.Kernel.SendAsync(new SubmitCode(@"
let d = 567", Language.FSharp.LanguageName()));


            var request = new
            {
                csharp = new[] { "a", "f", "b" },
                fsharp = new[] { "d" }
            };

            var response = await _server.HttpClient.PostAsync("/variables/",new StringContent(JsonConvert.SerializeObject( request)));

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
            await _server.Kernel.SendAsync(new SubmitCode(@"
var a = 123;
var b = ""1/2/3"";
var f = new { Field= ""string value""};", Language.CSharp.LanguageName()));


            var request = new
            {
                csharp = new[] { "a", "f", "b" }

            };

            var response = await _server.HttpClient.PostAsync("/variables/", new StringContent(JsonConvert.SerializeObject(request)));

            response.Content.Headers.ContentType.MediaType.Should().Be("application/json");
        }

        [Fact]
        public async Task Variable_serialization_can_be_customized_using_Formatter()
        {
            Formatter<FileInfo>.Register(
                info => new { TheName = info.Name }.SerializeToJson().Value,
                JsonFormatter.MimeType);

            await _server.Kernel.SendAsync(new SubmitCode("var theFile = new System.IO.FileInfo(\"the-file.txt\");"));

            var response = await _server.HttpClient.GetAsync("/variables/csharp/theFile");

            var responseContent = await response.Content.ReadAsStringAsync();

            var value = JObject.Parse(responseContent);

            value["TheName"].Value<string>().Should().Be("the-file.txt");
        }

        [Theory]
        [InlineData(Language.CSharp, "var a = 123;")]
        [InlineData(Language.FSharp, "let a = 123")]
        public async Task variable_is_returned_with_application_json_content_type(Language language, string code)
        {
            await _server.Kernel.SendAsync(new SubmitCode(code, language.LanguageName()));

            var response = await _server.HttpClient.GetAsync($"/variables/{language.LanguageName()}/a");

            response.Content.Headers.ContentType.MediaType.Should().Be("application/json");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task When_variable_does_not_exist_then_it_returns_404(Language language)
        {
            var response = await _server.HttpClient.GetAsync($"/variables/{language.LanguageName()}/does_not_exist");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task When_subkernel_does_not_exist_then_it_returns_404()
        {
            var response = await _server.HttpClient.GetAsync("/variables/does_not_exist/does_not_exist");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task can_get_static_content()
        {
            var response = await _server.HttpClient.GetAsync("/resources/logo-32x32.png");

            response.Content.Headers.ContentType.MediaType.Should().Be("image/png");
        }

        [Fact]
        public async Task can_get_kernel_names()
        {
            var response = await _server.HttpClient.GetAsync("/kernels");

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
            var response = await _server.HttpClient.GetAsync("/resources/dotnet-interactive.js");

            response.Content.Headers.ContentType.MediaType.Should().Be("application/javascript");
        }
    }
}
