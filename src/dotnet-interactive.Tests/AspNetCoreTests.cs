// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.Extensions.DependencyInjection;
using Pocket;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class AspNetCoreTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public void Dispose()
        {
            _disposables.Add(Formatting.Formatter.ResetToDefault);
            _disposables.Dispose();
        }

        private async Task<InProcessTestServer> GetServer(Language defaultLanguage = Language.CSharp, Action<IServiceCollection> servicesSetup = null, string command = "http", int port = 4242)
        {
            var newServer =
                await InProcessTestServer.StartServer(
                    $"{command} --default-kernel {defaultLanguage.LanguageName()} --http-port {port}", servicesSetup);

            _disposables.Add(newServer);

            return newServer;
        }

        [Fact]
        public async Task can_define_aspnet_endpoint_with_MapGet()
        {
            var server = await GetServer();

            var result = await server.Kernel.SendAsync(new SubmitCode(@"
#!aspnet

Endpoints.MapGet(""/"", async context =>
{
    await context.Response.WriteAsync($""Hello from MapGet!"");
});

await HttpClient.GetAsync(""/"")"));

            result.KernelEvents.ToSubscribedList().Should().NotContainErrors()
                .And.ContainSingle<ReturnValueProduced>()
                .Which.FormattedValues.Should().ContainSingle(f => f.MimeType == "text/html")
                .Which.Value.Should().Contain("Hello from MapGet!");
        }

        [Fact]
        public async Task can_redefine_aspnet_endpoint_with_MapInteractive()
        {
            var server = await GetServer();

            var result = await server.Kernel.SendAsync(new SubmitCode(@"
#!aspnet

Endpoints.MapGet(""/"", async context =>
{
    await context.Response.WriteAsync($""Hello from MapGet!"");
});

Endpoints.MapInteractive(""/"", async context =>
{
    await context.Response.WriteAsync($""Hello from MapInteractive!"");
});

Endpoints.MapInteractive(""/"", async context =>
{
    await context.Response.WriteAsync($""Hello from MapInteractive 2!"");
});

await HttpClient.GetAsync(""/"")"));

            result.KernelEvents.ToSubscribedList().Should().NotContainErrors()
                .And.ContainSingle<ReturnValueProduced>()
                .Which.FormattedValues.Should().ContainSingle(f => f.MimeType == "text/html")
                .Which.Value.Should().Contain("Hello from MapInteractive 2!");
        }

        [Fact]
        public async Task can_define_aspnet_middleware_with_Use()
        {
            var server = await GetServer();

            var result = await server.Kernel.SendAsync(new SubmitCode(@"
#!aspnet

App.Use(next =>
{
    return async httpContext =>
    {
        await httpContext.Response.WriteAsync(""Hello from middleware!"");
    };
});

await HttpClient.GetAsync(""/"")"));

            result.KernelEvents.ToSubscribedList().Should().NotContainErrors()
                .And.ContainSingle<ReturnValueProduced>()
                .Which.FormattedValues.Should().ContainSingle(f => f.MimeType == "text/html")
                .Which.Value.Should().Contain("Hello from middleware!");
        }

       [Fact]
        public async Task endpoints_take_precedence_over_new_middleware()
        {
            var server = await GetServer();

            var result = await server.Kernel.SendAsync(new SubmitCode(@"
#!aspnet

App.Use(next =>
{
    return async httpContext =>
    {
        await httpContext.Response.WriteAsync(""Hello from middleware!"");
    };
});

Endpoints.MapGet(""/"", async context =>
{
    await context.Response.WriteAsync($""Hello from MapGet!"");
});

await HttpClient.GetAsync(""/"")"));

            result.KernelEvents.ToSubscribedList().Should().NotContainErrors()
                .And.ContainSingle<ReturnValueProduced>()
                .Which.FormattedValues.Should().ContainSingle(f => f.MimeType == "text/html")
                .Which.Value.Should().Contain("Hello from MapGet!");

            // Re-adding the middleware makes no difference since it's added to the end of the pipeline.
            var result2 = await server.Kernel.SendAsync(new SubmitCode(@"
#!aspnet

App.Use(next =>
{
    return async httpContext =>
    {
        await httpContext.Response.WriteAsync(""Hello from middleware!"");
    };
});

await HttpClient.GetAsync(""/"")"));

            result2.KernelEvents.ToSubscribedList().Should().NotContainErrors()
                .And.ContainSingle<ReturnValueProduced>()
                .Which.FormattedValues.Should().ContainSingle(f => f.MimeType == "text/html")
                .Which.Value.Should().Contain("Hello from MapGet!");
        }

        [Fact]
        public async Task repeatedly_invoking_aspnet_command_noops()
        {
            var server = await GetServer();

            var result = await server.Kernel.SendAsync(new SubmitCode(@"
#!aspnet
#!aspnet

Endpoints.MapGet(""/"", async context =>
{
    await context.Response.WriteAsync($""Hello from MapGet!"");
});

await HttpClient.GetAsync(""/"")"));

            result.KernelEvents.ToSubscribedList().Should().NotContainErrors()
                .And.ContainSingle<ReturnValueProduced>()
                .Which.FormattedValues.Should().ContainSingle(f => f.MimeType == "text/html")
                .Which.Value.Should().Contain("Hello from MapGet!");
        }

        [Fact]
        public async Task aspnet_command_is_only_necessary_in_first_submission()
        {
            var server = await GetServer();

            var commandResult = await server.Kernel.SendAsync(new SubmitCode("#!aspnet"));

            commandResult.KernelEvents.ToSubscribedList().Should().NotContainErrors();

            var result = await server.Kernel.SendAsync(new SubmitCode(@"
Endpoints.MapGet(""/"", async context =>
{
    await context.Response.WriteAsync($""Hello from MapGet!"");
});

await HttpClient.GetAsync(""/"")"));

            result.KernelEvents.ToSubscribedList().Should().NotContainErrors()
                .And.ContainSingle<ReturnValueProduced>()
                .Which.FormattedValues.Should().ContainSingle(f => f.MimeType == "text/html")
                .Which.Value.Should().Contain("Hello from MapGet!");
        }

        [Fact]
        public async Task result_includes_trace_level_logs()
        {
            var server = await GetServer();

            var commandResult = await server.Kernel.SendAsync(new SubmitCode("#!aspnet"));

            commandResult.KernelEvents.ToSubscribedList().Should().NotContainErrors();

            var result = await server.Kernel.SendAsync(new SubmitCode(@"
#!aspnet

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Endpoints.MapGet(""/"", async httpContext =>
{
    var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger(""interactive"");
    logger.LogTrace(""Log from MapGet!"");

    await httpContext.Response.WriteAsync(""Hello from MapGet!"");
});

await HttpClient.GetAsync(""/"")"));

            result.KernelEvents.ToSubscribedList().Should().NotContainErrors()
                .And.ContainSingle<ReturnValueProduced>()
                .Which.FormattedValues.Should().ContainSingle(f => f.MimeType == "text/html")
                .Which.Value.Should().Contain("Log from MapGet!");
        }

        [Fact]
        public async Task server_listens_on_ephemeral_port()
        {
            var server = await GetServer();

            var result = await server.Kernel.SendAsync(new SubmitCode(@"
#!aspnet

HttpClient.BaseAddress"));

            // Assume any port higher than 10000 is ephemeral. In practice, the start of the ephemeral port range is
            // usually even higher (Windows XP and older Windows releases notwithstanding).
            // https://en.wikipedia.org/wiki/Ephemeral_port
            var serverUri = result.KernelEvents.ToSubscribedList().Should().NotContainErrors()
                .And.ContainSingle<ReturnValueProduced>().Which.Value.Should().Match(uri => uri.As<Uri>().Port > 10_000);
        }
    }
}
