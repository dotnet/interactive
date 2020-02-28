// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
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
            _server = InProcessTestServer.StartServer("stdio --default-kernel csharp");
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        [Fact]
        public async Task can_get_variable_value()
        {
            await _server.Kernel.SendAsync(new SubmitCode("var a = 123;", "csharp"));

            var response = await _server.HttpClient.GetAsync("/variables/csharp/a");

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();

            var value = JToken.Parse(responseContent).Value<int>();

            value.Should().Be(123);
        }

        [Fact]
        public async Task variable_is_returned_with_application_json_content_type()
        {
            await _server.Kernel.SendAsync(new SubmitCode("var a = 123;", "csharp"));

            var response = await _server.HttpClient.GetAsync("/variables/csharp/a");

            response.EnsureSuccessStatusCode();

            response.Content.Headers.ContentType.MediaType.Should().Be("application/json");
        }

        [Fact]
        public async Task When_variable_does_not_exist_then_it_returns_404()
        {
            var response = await _server.HttpClient.GetAsync("/variables/csharp/does_not_exist");

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
    }
}