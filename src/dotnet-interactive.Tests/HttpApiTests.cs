// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.App.Connection;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Pocket;

using Xunit;
using Xunit.Abstractions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Microsoft.DotNet.Interactive.App.Tests;

public class HttpApiTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public HttpApiTests(ITestOutputHelper output)
    {
        _disposables.Add(Formatting.Formatter.ResetToDefault);
        _disposables.Add(output.SubscribeToPocketLogger());
    }
    public void Dispose()
    {
        _disposables.Dispose();
    }

    private async Task<InProcessTestServer> GetServer(Language defaultLanguage = Language.CSharp, Action<IServiceCollection> servicesSetup = null, string command = "stdio", int port = 4242)
    {
        var newServer =
            await InProcessTestServer.StartServer(
                $"{command} --default-kernel {defaultLanguage.LanguageName()} --http-port {port}", servicesSetup);

        _disposables.Add(newServer);

        return newServer;
    }

    [Fact(Skip = "Revisit jupyter integration")]
    public async Task discovery_route_is_not_registered_without_JupyterFrontedEnvironment()
    {
        var server = await GetServer();
        var response = await server.HttpClient.PostAsync("/discovery", new StringContent("http://choosen.one:1000/"));

        using var scope = new AssertionScope();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var frontendEnvironment = server.FrontendEnvironment;
        frontendEnvironment.Should().NotBeOfType<HtmlNotebookFrontendEnvironment>();
    }

    [Fact]
    public async Task FrontendEnvironment_host_is_set_via_handshake()
    {
        var tunnelUri = new Uri("http://choosen.one:1000/");
        var server = await GetServer(command: "stdio", port: 1000);
        var response = await server.HttpClient.PostAsync("/discovery", new StringContent(tunnelUri.AbsoluteUri));
        using var scope = new AssertionScope();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var frontendEnvironment = (HtmlNotebookFrontendEnvironment)server.FrontendEnvironment;

        var apiUri = await frontendEnvironment.GetApiUriAsync();
        apiUri.Should().Be(tunnelUri);
    }

    [Fact]
    public async Task HttpApiTunneling_configures_frontend_environment()
    {
        var tunnelUri = new Uri("http://choosen.one:1000/");
        var server = await GetServer(command: "stdio", port: 1000);

        var response = await server.HttpClient.PostAsync("/apitunnel", new StringContent(JsonSerializer.Serialize(new { tunnelUri = tunnelUri.AbsoluteUri, frontendType = "vscode" })));
        using var scope = new AssertionScope();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var frontendEnvironment = (HtmlNotebookFrontendEnvironment) server.FrontendEnvironment;

        var apiUri = await frontendEnvironment.GetApiUriAsync();
        apiUri.Should().Be(tunnelUri);
    }

    [Fact]
    public async Task HttpApiTunneling_return_bootstrapper_js_url()
    {
        var tunnelUri = new Uri("http://choosen.one:1000/");
        var server = await GetServer(command: "stdio", port: 1000);

        var response = await server.HttpClient.PostAsync("/apitunnel", new StringContent(JsonSerializer.Serialize(new { tunnelUri = tunnelUri.AbsoluteUri, frontendType = "vscode" })));

        using var scope = new AssertionScope();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.MediaType.Should().Be("text/plain");

        var match = $"{tunnelUri.AbsoluteUri}apitunnel/*/bootstrapper.js";
        var responseBody = JObject.Parse(await response.Content.ReadAsStringAsync());
        responseBody["bootstrapperUri"].Value<string>()
            .Should().Match(match);
    }

    [Fact]
    public async Task HttpApiTunneling_route_serves_bootstrapper_js()
    {
        var tunnelUri = new Uri("http://choosen.one:1000/");
        var server = await GetServer(command: "stdio", port: 1000);

        var response = await server.HttpClient.PostAsync("/apitunnel", new StringContent(JsonSerializer.Serialize(new { tunnelUri = tunnelUri.AbsoluteUri, frontendType = "vscode" })));
        var responseBody = JObject.Parse(await response.Content.ReadAsStringAsync());

        var boostrapperUri = responseBody["bootstrapperUri"].Value<string>();


        response = await server.HttpClient.GetAsync(boostrapperUri);
        var code = await response.Content.ReadAsStringAsync();
        using var scope = new AssertionScope();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.MediaType.Should().Be("text/javascript");

        code.Should().Contain(tunnelUri.AbsoluteUri);
        code.Should().Contain("bootstrapper_vscode_");

    }

    [Theory]
    [InlineData(Language.CSharp, "var a = 123;")]
    [InlineData(Language.FSharp, "let a = 123")]
    public async Task can_get_variable_value(Language language, string code)
    {
        var server = await GetServer();
        await server.Kernel.SendAsync(new SubmitCode(code, language.LanguageName()));

        var response = await server.HttpClient.GetAsync($"/variables/{language.LanguageName()}/a");

        await response.ShouldSucceed();

        var responseContent = await response.Content.ReadAsStringAsync();

        responseContent.Should().BeJsonEquivalentTo(123);
    }

    [Theory]
    [InlineData(Language.CSharp, "var a = \"Code value\";")]
    [InlineData(Language.FSharp, "let a = \"Code value\"")]
    public async Task can_get_variable_value_when_variable_is_string(Language language, string code)
    {
        var server = await GetServer(language);

        await server.Kernel.SendAsync(new SubmitCode(code, language.LanguageName()));

        var response = await server.HttpClient.GetAsync($"/variables/{language.LanguageName()}/a");

        var responseContent = await response.Content.ReadAsStringAsync();

        responseContent.Should().BeJsonEquivalentTo("Code value");
    }

    [Theory]
    [InlineData(Language.CSharp, "var a = \"Code value\";")]
    [InlineData(Language.FSharp, "let a = \"Code value\"")]
    public async Task deferred_command_produce_html_bootstrap_code(Language language, string code)
    {
        var server = await GetServer(language);
        var events = server.Kernel.KernelEvents.ToSubscribedList();
        await server.Kernel.SendAsync(new SubmitCode(code, language.LanguageName()));



        events.Should()
            .ContainSingle<DisplayedValueProduced>()
            .Which
            .FormattedValues
            .Should()
            .ContainSingle(v => v.MimeType == HtmlFormatter.MimeType)
            .Which
            .Value
            .Should()
            .Contain("<script type='text/javascript'>");
    }

    [Fact]
    public async Task can_get_variables_with_bulk_request()
    {
        var server = await GetServer();
        await server.Kernel.SendAsync(new SubmitCode(@"
var a = 123;
var b = ""1/2/3"";
var f = new { Field= ""string value""};", Language.CSharp.LanguageName()));

        await server.Kernel.SendAsync(new SubmitCode(@"
let d = 567", Language.FSharp.LanguageName()));


        var request = new
        {
            csharp = new[] { "a", "f", "b" },
            fsharp = new[] { "d" }
        };

        var response = await server.HttpClient.PostAsync("/variables/", new StringContent(JsonConvert.SerializeObject(request)));

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
    public async Task get_variables_preserves_property_case()
    {
        var server = await GetServer();
        await server.Kernel.SendAsync(new SubmitCode(@"
var f = new { Field= ""string value""};", Language.CSharp.LanguageName()));


        var response = await server.HttpClient.GetAsync("/variables/csharp/f");

        var responseContent = await response.Content.ReadAsStringAsync();

        var responseObject = JObject.Parse(responseContent);

        responseObject.Properties()
            .Select(p => p.Name)
            .Should()
            .ContainSingle("Field");
    }

    [Fact]
    public async Task bulk_variable_request_is_returned_with_application_json_content_type()
    {
        var server = await GetServer();
        await server.Kernel.SendAsync(new SubmitCode(@"
var a = 123;
var b = ""1/2/3"";
var f = new { Field= ""string value""};", Language.CSharp.LanguageName()));


        var request = new
        {
            csharp = new[] { "a", "f", "b" }

        };

        var response = await server.HttpClient.PostAsync("/variables/", new StringContent(JsonConvert.SerializeObject(request)));

        response.Content.Headers.ContentType.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task Variable_serialization_can_be_customized_using_Formatter()
    {
        Formatting.Formatter.Register<FileInfo>(
            info => JsonSerializer.Serialize(new { TheName = info.Name }),
            JsonFormatter.MimeType);

        var server = await GetServer();

        await server.Kernel.SendAsync(new SubmitCode("var theFile = new System.IO.FileInfo(\"the-file.txt\");"));

        var response = await server.HttpClient.GetAsync("/variables/csharp/theFile");

        var responseContent = await response.Content.ReadAsStringAsync();

        var value = JObject.Parse(responseContent);

        value["TheName"].Value<string>().Should().Be("the-file.txt");
    }

    [Theory]
    [InlineData(Language.CSharp, "var a = 123;")]
    [InlineData(Language.FSharp, "let a = 123")]
    public async Task variable_is_returned_with_application_json_content_type(Language language, string code)
    {
        var server = await GetServer();

        await server.Kernel.SendAsync(new SubmitCode(code, language.LanguageName()));

        var response = await server.HttpClient.GetAsync($"/variables/{language.LanguageName()}/a");

        await response.ShouldSucceed();

        response.Content.Headers.ContentType.MediaType.Should().Be("application/json");
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task When_variable_does_not_exist_then_it_returns_404(Language language)
    {
        var server = await GetServer();
        var response = await server.HttpClient.GetAsync($"/variables/{language.LanguageName()}/does_not_exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task When_subkernel_does_not_exist_then_it_returns_404()
    {
        var server = await GetServer();
        var response = await server.HttpClient.GetAsync("/variables/does_not_exist/does_not_exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task can_get_static_content()
    {
        var server = await GetServer();
        var response = await server.HttpClient.GetAsync("/resources/logo-32x32.png");

        await response.ShouldSucceed();

        response.Content.Headers.ContentType.MediaType.Should().Be("image/png");
    }

    [Fact]
    public async Task can_get_static_content_from_extensions()
    {
        var server = await GetServer();
        var kernel = server.Kernel;

        var extensionPackage = await KernelExtensionTestHelper.GetFileProviderExtensionAsync();

        await kernel.SubmitCodeAsync($@"#i ""nuget:{extensionPackage.PackageLocation}""
#r ""nuget:{extensionPackage.Name},{extensionPackage.Version}""");

        var response = await server.HttpClient.GetAsync("extensions/TestKernelExtension/resources/file.txt");

        await response.ShouldSucceed();

        response.Content.Headers.ContentType.MediaType.Should().Be("text/plain");
    }

    [Fact]
    public async Task can_get_kernel_names()
    {
        var server = await GetServer();
        var response = await server.HttpClient.GetAsync("/kernels");

        await response.ShouldSucceed();

        response.Content.Headers.ContentType.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var kernels = JToken.Parse(responseContent).Values<string>();

        kernels.Should()
            .BeEquivalentTo(
                ".NET",
                "sql",
                "kql",
                "csharp",
                "fsharp",
                "pwsh",
                "html",
                "http",
                "javascript",
                "mermaid",
                "value");
    }

    [Fact]
    public async Task stdio_mode_returns_javascript_api_via_http()
    {
        var port = GetFreePort();

        using var kernel = new CompositeKernel();
                
        kernel.AddKernelConnector(new ConnectStdIoDirective(new Uri("kernel://test-kernel")));

        await kernel.SendAsync(new SubmitCode($"#!connect stdio --kernel-name proxy --command \"{Dotnet.Path}\" \"{typeof(Program).Assembly.Location}\" stdio --http-port {port}"));

        using var client = new HttpClient();

        var response = await client.GetAsync($"http://localhost:{port}/resources/dotnet-interactive.js");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        response.Content.Headers.ContentType.MediaType.Should().Be("text/javascript");
    }

    private static int GetFreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}

internal static class HttpClientTestExtensions
{
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