// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pocket;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Browser.Tests;

[TestClass]
public class HtmlKernelTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public HtmlKernelTests(TestContext output)
    {
        _disposables.Add(output.SubscribeToPocketLogger());
    }

    public void Dispose() => _disposables.Dispose();

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Requires Playwright installed
    [TestMethod]
    public async Task It_can_share_the_underlying_Playwright_page_object()
    {
        using var kernel = await CreateHtmlProxyKernelAsync();

        var result = await kernel.SendAsync(new RequestValue("*"));

        result.Events
              .Should()
              .ContainSingle<ValueProduced>()
              .Which
              .Value
              .Should()
              .BeAssignableTo<ILocator>();
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Requires Playwright installed
    [TestMethod]
    public async Task It_can_share_the_underlying_page_HTML()
    {
        using var kernel = await CreateHtmlProxyKernelAsync();

        var setupEvents = await kernel.SubmitCodeAsync("<div>hello</div>");
        setupEvents.Events.Should().NotContainErrors();

        var result = await kernel.SendAsync(new RequestValue("*", "text/html"));

        result.Events
              .Should()
              .ContainSingle<ValueProduced>()
              .Which
              .FormattedValue
              .Value
              .Should()
              .Contain("<div>hello</div>");
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Requires Playwright installed
    [TestMethod]
    public async Task It_can_share_the_underlying_page_content()
    {
        using var kernel = await CreateHtmlProxyKernelAsync();

        var setupEvents = await kernel.SubmitCodeAsync("<div>hello</div>");
        setupEvents.Events.Should().NotContainErrors();

        var result = await kernel.SendAsync(new RequestValue(":root", "text/plain"));

        result.Events
              .Should()
              .ContainSingle<ValueProduced>()
              .Which
              .FormattedValue
              .Value
              .Should()
              .Be("hello");
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Requires Playwright installed
    [TestMethod]
    public async Task It_can_capture_a_PNG_using_a_selector()
    {
        using var kernel = await CreateHtmlProxyKernelAsync();

        await kernel.SubmitCodeAsync(@"<svg height=""250"" width=""450"">
  <polygon points=""225,10 100,210 350,210"" style=""fill:rgb(0,0,0);stroke:#609AAF;stroke-width:10""></polygon>
</svg>");

        var result = await kernel.SendAsync(new RequestValue("svg", "image/png"));

        result.Events.Should().NotContainErrors();

        var value = result.Events
                          .Should()
                          .ContainSingle<ValueProduced>()
                          .Which
                          .FormattedValue
                          .Value;

        value.Invoking(v => SkiaSharp.SKImage.FromEncodedData(Convert.FromBase64String(v)))
             .Should()
             .NotThrow();
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Requires Playwright installed
    [TestMethod]
    public async Task It_can_capture_a_Jpeg_using_a_selector()
    {
        using var kernel = await CreateHtmlProxyKernelAsync();

        await kernel.SubmitCodeAsync(@"<svg height=""250"" width=""450"">
  <polygon points=""225,10 100,210 350,210"" style=""fill:rgb(0,0,0);stroke:#609AAF;stroke-width:10""></polygon>
</svg>");

        var result = await kernel.SendAsync(new RequestValue("svg", "image/jpeg"));

        result.Events.Should().NotContainErrors();

        var value = result.Events
                          .Should()
                          .ContainSingle<ValueProduced>()
                          .Which
                          .FormattedValue
                          .Value;

        value.Invoking(v =>
                SkiaSharp.SKImage.FromEncodedData(Convert.FromBase64String(v)))
             .Should()
             .NotThrow();

    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Requires Playwright installed
    [TestMethod]
    public async Task It_has_shareable_values()
    {
        using var kernel = await CreateHtmlProxyKernelAsync();

        var result = await kernel.SendAsync(new RequestValueInfos());

        result.Events
              .Should()
              .ContainSingle<ValueInfosProduced>()
              .Which
              .ValueInfos
              .Should()
              .ContainSingle(i => i.Name == ":root");
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Requires Playwright installed
    [TestMethod]
    public async Task HTML_kernel_can_see_DOM_changes_made_by_JavaScript_kernel()
    {
        var connector = new PlaywrightKernelConnector(!Debugger.IsAttached);

        using var javascriptKernel = await connector.CreateKernelAsync("javascript", BrowserKernelLanguage.JavaScript);
        using var htmlKernel = await connector.CreateKernelAsync("html", BrowserKernelLanguage.Html);

        await javascriptKernel.SendAsync(new SubmitCode("document.body.innerHTML += '<div>howdy</div>'"));

        var result = await htmlKernel.SendAsync(new RequestValue("html", "text/html"));

        result.Events
              .Should()
              .ContainSingle<ValueProduced>()
              .Which
              .FormattedValue
              .Value
              .Should()
              .Contain("<div>howdy</div>");
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Requires Playwright installed
    [TestMethod]
    public async Task JavaScript_kernel_can_see_DOM_changes_made_by_HTML_kernel()
    {
        var connector = new PlaywrightKernelConnector(!Debugger.IsAttached);

        using var javascriptKernel = await connector.CreateKernelAsync("javascript", BrowserKernelLanguage.JavaScript);
        using var htmlKernel = await connector.CreateKernelAsync("html", BrowserKernelLanguage.Html);

        await htmlKernel.SendAsync(new SubmitCode("<div>hey there!</div>"));

        var result = await javascriptKernel.SendAsync(new SubmitCode("return document.body.innerHTML;"));

        result.Events.Should().NotContainErrors();

        result.Events
              .Should()
              .ContainSingle<ReturnValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(v => v.Value.Contains("<div>hey there!</div>"));
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Requires Playwright installed
    [TestMethod]
    public async Task html_kernel_evaluates_script_tags()
    {
        var connector = new PlaywrightKernelConnector(!Debugger.IsAttached);
        
        using var htmlKernel = await connector.CreateKernelAsync("html", BrowserKernelLanguage.Html);
        using var javascriptKernel = await connector.CreateKernelAsync("javascript", BrowserKernelLanguage.JavaScript);
        await htmlKernel.SendAsync(new SubmitCode("<div><script>myValue = 123;</script></div>"));

        var result = await javascriptKernel.SendAsync(new SubmitCode("return myValue;"));

        result.Events.Should().NotContainErrors();

        result.Events
              .Should()
              .ContainSingle<ReturnValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(v => v.Value.Contains("123"));
    }

    private async Task<Kernel> CreateHtmlProxyKernelAsync()
    {
        var connector = new PlaywrightKernelConnector(!Debugger.IsAttached);

        var proxy = await connector.CreateKernelAsync("html", BrowserKernelLanguage.Html);

        return proxy;
    }
}