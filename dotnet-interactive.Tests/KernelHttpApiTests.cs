// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Tests;
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
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        [Fact]
        public async Task FrontendEnvrionment_host_is_set_via_handshake()
        {
            var expectedUri = new Uri("http://choosen.one:1000/");
            var response = await _server.HttpClient.PostAsync("/channelhandshake", new StringContent(expectedUri.AbsoluteUri));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            _server.FrontendEnvironment.Host.Should().Be(expectedUri);
        }

        [Theory]
        [InlineData(Language.CSharp, "var a = 123;")]
        [InlineData(Language.FSharp, "let a = 123")]
        public async Task can_get_variable_value(Language language, string code)
        {
            await _server.Kernel.SendAsync(new SubmitCode(code, language.LanguageName()));

            var response = await _server.HttpClient.GetAsync($"/variables/{language.LanguageName()}/a");

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();

            var value = JToken.Parse(responseContent).Value<int>();

            value.Should().Be(123);
        }

        [Theory]
        [InlineData(Language.CSharp, "var a = 123;")]
        [InlineData(Language.FSharp, "let a = 123")]
        public async Task variable_is_returned_with_application_json_content_type(Language language, string code)
        {
            await _server.Kernel.SendAsync(new SubmitCode(code, language.LanguageName()));

            var response = await _server.HttpClient.GetAsync($"/variables/{language.LanguageName()}/a");

            response.EnsureSuccessStatusCode();

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

            response.EnsureSuccessStatusCode();

            response.Content.Headers.ContentType.MediaType.Should().Be("image/png");
        }

        [Fact]
        public async Task can_get_kernel_names()
        {
            var response = await _server.HttpClient.GetAsync("/kernels");

            response.EnsureSuccessStatusCode();

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

            response.EnsureSuccessStatusCode();

            response.Content.Headers.ContentType.MediaType.Should().Be("application/javascript");
        }
    }
}
