// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Pocket.For.Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Browser.Tests;

[LogToPocketLogger(FileNameEnvironmentVariable = "POCKETLOGGER_LOG_PATH")]
public class JavaScriptKernelTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public JavaScriptKernelTests(ITestOutputHelper output)
    {
        _disposables.Add(output.SubscribeToPocketLogger());
    }

    public void Dispose() => _disposables.Dispose();

    [FactSkipLinux]
    public async Task It_can_execute_code()
    {
        using var kernel = await CreateJavaScriptProxyKernelAsync();

        var result = await kernel.SendAsync(new SubmitCode("x = [ 1, 2, 3 ]"));

        var events = result.KernelEvents.ToSubscribedList();

        events.Should().NotContainErrors();
    }

    [FactSkipLinux]
    public async Task It_can_get_a_return_value()
    {
        using var kernel = await CreateJavaScriptProxyKernelAsync();

        var result = await kernel.SendAsync(new SubmitCode("x = 123;\nreturn x;"));

        var events = result.KernelEvents.ToSubscribedList();

        events.Should().ContainSingle<ReturnValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(v => v.MimeType == "application/json" &&
                                  v.Value == "123");
    }

    [FactSkipLinux]
    public async Task It_can_get_console_log_output()
    {
        using var kernel = await CreateJavaScriptProxyKernelAsync();

        var result = await kernel.SendAsync(new SubmitCode("console.log(123);"));

        var events = result.KernelEvents.ToSubscribedList();

        events.Should().ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(v => v.MimeType == "text/plain" &&
                                  v.Value == "123");
    }

    private static async Task<Kernel> CreateJavaScriptProxyKernelAsync()
    {
        var connector = new PlaywrightKernelConnector();

        var proxy = await connector.CreateKernelAsync("javascript");

        return proxy;
    }
}