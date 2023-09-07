// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.HttpRequest;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.AspNetCore.Tests;

public class AspNetCoreTests : IDisposable
{
    private readonly CompositeKernel _kernel;

    public AspNetCoreTests()
    {
        _kernel = new CompositeKernel
        {
            new CSharpKernel(),
        };

        var loadTask = new AspNetCoreKernelExtension().OnLoadAsync(_kernel);
        Assert.Same(Task.CompletedTask, loadTask);

        HttpResponseMessageFormattingExtensions.RegisterFormatters();
    }

    public void Dispose()
    {
        _kernel.Dispose();
    }

    [Fact]
    public async Task can_define_aspnet_endpoint_with_MapGet()
    {
        var result = await _kernel.SendAsync(new SubmitCode(@"
#!aspnet

Endpoints.MapGet(""/"", async context =>
{
    await context.Response.WriteAsync($""Hello from MapGet!"");
});

await HttpClient.GetAsync(""/"")"));

        result.Events.Should().NotContainErrors()
            .And.ContainSingle<ReturnValueProduced>()
            .Which.FormattedValues.Should().ContainSingle(f => f.MimeType == "text/html")
            .Which.Value.Should().Contain("Hello from MapGet!");
    }

    [Fact]
    public async Task can_redefine_aspnet_endpoint_with_MapInteractive()
    {
        var result = await _kernel.SendAsync(new SubmitCode(@"
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

        result.Events.Should().NotContainErrors()
            .And.ContainSingle<ReturnValueProduced>()
            .Which.FormattedValues.Should().ContainSingle(f => f.MimeType == "text/html")
            .Which.Value.Should().Contain("Hello from MapInteractive 2!");
    }

    [Fact]
    public async Task can_define_aspnet_middleware_with_Use()
    {
        var result = await _kernel.SendAsync(new SubmitCode(@"
#!aspnet

App.Use(next =>
{
    return async httpContext =>
    {
        await httpContext.Response.WriteAsync(""Hello from middleware!"");
    };
});

await HttpClient.GetAsync(""/"")"));

        result.Events.Should().NotContainErrors()
            .And.ContainSingle<ReturnValueProduced>()
            .Which.FormattedValues.Should().ContainSingle(f => f.MimeType == "text/html")
            .Which.Value.Should().Contain("Hello from middleware!");
    }

    [Fact]
    public async Task endpoints_take_precedence_over_new_middleware()
    {
        var result = await _kernel.SendAsync(new SubmitCode(@"
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

        result.Events.Should().NotContainErrors()
            .And.ContainSingle<ReturnValueProduced>()
            .Which.FormattedValues.Should().ContainSingle(f => f.MimeType == "text/html")
            .Which.Value.Should().Contain("Hello from MapGet!");

        // Re-adding the middleware makes no difference since it's added to the end of the pipeline.
        var result2 = await _kernel.SendAsync(new SubmitCode(@"
#!aspnet

App.Use(next =>
{
    return async httpContext =>
    {
        await httpContext.Response.WriteAsync(""Hello from middleware!"");
    };
});

await HttpClient.GetAsync(""/"")"));

        result2.Events.Should().NotContainErrors()
            .And.ContainSingle<ReturnValueProduced>()
            .Which.FormattedValues.Should().ContainSingle(f => f.MimeType == "text/html")
            .Which.Value.Should().Contain("Hello from MapGet!");
    }

    [Fact]
    public async Task repeatedly_invoking_aspnet_command_noops()
    {
        var result = await _kernel.SendAsync(new SubmitCode(@"
#!aspnet
#!aspnet

Endpoints.MapGet(""/"", async context =>
{
    await context.Response.WriteAsync($""Hello from MapGet!"");
});

await HttpClient.GetAsync(""/"")"));

        result.Events.Should().NotContainErrors()
            .And.ContainSingle<ReturnValueProduced>()
            .Which.FormattedValues.Should().ContainSingle(f => f.MimeType == "text/html")
            .Which.Value.Should().Contain("Hello from MapGet!");
    }

    [Fact]
    public async Task aspnet_command_is_only_necessary_in_first_submission()
    {
        var commandResult = await _kernel.SendAsync(new SubmitCode("#!aspnet"));

        commandResult.Events.Should().NotContainErrors();

        var result = await _kernel.SendAsync(new SubmitCode(@"
Endpoints.MapGet(""/"", async context =>
{
    await context.Response.WriteAsync($""Hello from MapGet!"");
});

await HttpClient.GetAsync(""/"")"));

        result.Events.Should().NotContainErrors()
            .And.ContainSingle<ReturnValueProduced>()
            .Which.FormattedValues.Should().ContainSingle(f => f.MimeType == "text/html")
            .Which.Value.Should().Contain("Hello from MapGet!");
    }

    [Fact]
    public async Task server_listens_on_ephemeral_port()
    {
        var result = await _kernel.SendAsync(new SubmitCode(@"
#!aspnet

HttpClient.BaseAddress"));

        // Assume any port higher than 1000 is ephemeral. In practice, the start of the ephemeral port range is
        // usually even higher (Windows XP and older Windows releases notwithstanding).
        // https://en.wikipedia.org/wiki/Ephemeral_port
        result.Events.Should().NotContainErrors()
            .And.ContainSingle<ReturnValueProduced>()
            .Which.Value.Should().Match(uri => uri.As<Uri>().Port > 1_000);
    }
}