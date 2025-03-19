// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Browser.Tests;

[TestClass]
public class JavaScriptKernelTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public JavaScriptKernelTests(TestContext output)
    {
        _disposables.Add(output.SubscribeToPocketLogger());
    }

    public void Dispose() => _disposables.Dispose();

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Requires Playwright installed
    [TestMethod]
    public async Task It_can_execute_code()
    {
        using var kernel = await CreateJavaScriptProxyKernelAsync();

        var result = await kernel.SendAsync(new SubmitCode("x = [ 1, 2, 3 ]"));

        result.Events.Should().NotContainErrors();
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Requires Playwright installed
    [TestMethod]
    public async Task It_can_get_a_return_value()
    {
        using var kernel = await CreateJavaScriptProxyKernelAsync();

        var result = await kernel.SendAsync(new SubmitCode("x = 123;\nreturn x;"));

        result.Events.Should().ContainSingle<ReturnValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(v => v.MimeType == "application/json" &&
                                  v.Value == "123");
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Requires Playwright installed
    [TestMethod]
    public async Task It_can_get_console_log_output()
    {
        using var kernel = await CreateJavaScriptProxyKernelAsync();

        var result = await kernel.SendAsync(new SubmitCode("console.log(123);"));

        result.Events.Should().ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(v => v.MimeType == "text/plain" &&
                                  v.Value == "123");
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Requires Playwright installed
    [TestMethod]
    public async Task It_can_import_value_from_another_kernel()
    {
        using var kernel = await CreateJavaScriptProxyKernelAsync();
        var csharp = new CSharpKernel();
        csharp.UseValueSharing();

        var compositeKernel = new CompositeKernel
        {
            kernel,
            csharp
        };

        compositeKernel.DefaultKernelName = csharp.Name;

        await compositeKernel.SendAsync(new SubmitCode("var x = 123;", targetKernelName: csharp.Name));
        var result = await compositeKernel.SendAsync(new SubmitCode(@"#!share x --from csharp
console.log(x);", targetKernelName: kernel.Name));

        result.Events.Should().ContainSingle<DisplayedValueProduced>()
            .Which
            .FormattedValues
            .Should()
            .ContainSingle(v => v.MimeType == "text/plain" &&
                                v.Value == "123");
    }

    [OSCondition(ConditionMode.Exclude, OperatingSystems.Linux)] // Requires Playwright installed
    [TestMethod]
    public async Task It_can_share_value_with_another_kernel()
    {
        using var javascriptKernel = await CreateJavaScriptProxyKernelAsync();
        var csharpKernel = new CSharpKernel();
        csharpKernel.UseValueSharing();

        var compositeKernel = new CompositeKernel
        {
            javascriptKernel,
            csharpKernel
        };

        compositeKernel.DefaultKernelName = csharpKernel.Name;

        await compositeKernel.SendAsync(new SubmitCode("x = 123;", targetKernelName: javascriptKernel.Name));
        var result = await compositeKernel.SendAsync(new SubmitCode($"""
                                                                     #!share x --from {javascriptKernel.Name}
                                                                     System.Console.Write(x);
                                                                     """, targetKernelName: csharpKernel.Name));

        result.Events.Should().NotContainErrors();

        result.Events.Should().ContainSingle<StandardOutputValueProduced>()
            .Which
            .FormattedValues
            .Should()
            .ContainSingle(v => v.MimeType == "text/plain" &&
                                v.Value == "123");
    }

    private static async Task<Kernel> CreateJavaScriptProxyKernelAsync()
    {
        var connector = new PlaywrightKernelConnector(!Debugger.IsAttached, Debugger.IsAttached);

        var proxy = await connector.CreateKernelAsync("javascript", BrowserKernelLanguage.JavaScript);

        return proxy;
    }
}