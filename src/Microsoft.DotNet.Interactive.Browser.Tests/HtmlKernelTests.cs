// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.Playwright;
using Pocket;
using Pocket.For.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Browser.Tests;

[LogToPocketLogger(FileNameEnvironmentVariable = "POCKETLOGGER_LOG_PATH")]
public class HtmlKernelTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public HtmlKernelTests(ITestOutputHelper output)
    {
        _disposables.Add(output.SubscribeToPocketLogger());
    }

    public void Dispose() => _disposables.Dispose();

    [Fact]
    public async Task It_can_share_the_underlying_Playwright_page_object()
    {
        using var kernel = await CreateJavaScriptKernelAsync();

        var result = await kernel.SendAsync(new RequestValue("page", "text/html"));

        var events = result.KernelEvents.ToSubscribedList();

        using var _ = new AssertionScope();

        events
            .Should()
            .ContainSingle<ValueProduced>()
            .Which
            .Value
            .Should()
            .BeAssignableTo<IPage>();
    }

    [Fact]
    public async Task It_can_share_the_underlying_page_HTML()
    {
        using var kernel = await CreateJavaScriptKernelAsync();

        var setupEvents = await kernel.SubmitCodeAsync("<div>hello</div>");
        setupEvents.KernelEvents.ToSubscribedList().Should().NotContainErrors();

        var result = await kernel.SendAsync(new RequestValue("page", "text/html"));

        var events = result.KernelEvents.ToSubscribedList();

        using var _ = new AssertionScope();

        events
            .Should()
            .ContainSingle<ValueProduced>()
            .Which
            .FormattedValue
            .Value
            .Should()
            .Be("<head></head><body><div>hello</div></body>");
    }

    [Fact]
    public async Task It_can_share_the_underlying_page_content()
    {
        using var kernel = await CreateJavaScriptKernelAsync();

        var setupEvents = await kernel.SubmitCodeAsync("<div>hello</div>");
        setupEvents.KernelEvents.ToSubscribedList().Should().NotContainErrors();

        var result = await kernel.SendAsync(new RequestValue("page", "text/plain"));

        var events = result.KernelEvents.ToSubscribedList();

        using var _ = new AssertionScope();

        events
            .Should()
            .ContainSingle<ValueProduced>()
            .Which
            .FormattedValue
            .Value
            .Should()
            .Be("hello");
    }


    [Fact]
    public async Task It_has_shareable_values()
    {
        using var kernel = await CreateJavaScriptKernelAsync();

        var result = await kernel.SendAsync(new RequestValueInfos());

        var events = result.KernelEvents.ToSubscribedList();

        events
            .Should()
            .ContainSingle<ValueInfosProduced>()
            .Which
            .ValueInfos
            .Should()
            .ContainSingle(i => i.Name == "page");
    }


    // FIX: (HtmlKernelTests) 
    private async Task<Kernel> CreateJavaScriptKernelAsync()
    {
        var connector = new PlaywrightKernelConnector();

        var proxy = await connector.CreateKernelAsync("html");

        return proxy;
    }
}