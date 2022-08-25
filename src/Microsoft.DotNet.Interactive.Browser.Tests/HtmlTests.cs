// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Pocket.For.Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Browser.Tests;

[LogToPocketLogger(FileNameEnvironmentVariable = "POCKETLOGGER_LOG_PATH")]
public class HtmlTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public HtmlTests(ITestOutputHelper output)
    {
        _disposables.Add(output.SubscribeToPocketLogger());
    }

    public void Dispose() => _disposables.Dispose();

    [FactSkipLinux]
    public async Task It_can_execute_code()
    {
        using var kernel = await CreateKernelAsync();

        var result = await kernel.SendAsync(new SubmitCode(@"<div id=""target"">content</div>", "html"));

        var events = result.KernelEvents.ToSubscribedList();

        events.Should().NotContainErrors();
    }
    private static async Task<CompositeKernel> CreateKernelAsync()
    {
        var compositeKernel = new CompositeKernel();

        var connector = new PlaywrightKernelConnector();

        var proxy = await connector.CreateKernelAsync("html");
        compositeKernel.Add(proxy);

        return compositeKernel;
    }
}