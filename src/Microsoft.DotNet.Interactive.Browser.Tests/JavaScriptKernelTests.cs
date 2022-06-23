// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Browser.Tests;

public class JavaScriptTests
{
    [Fact]
    public async Task It_can_execute_code()
    {
        using var kernel = await CreateKernelAsync();

        var result = await kernel.SendAsync(new SubmitCode("x = [ 1, 2, 3 ]", "javascript"));

        var events = result.KernelEvents.ToSubscribedList();

        events.Should().ContainSingle<CommandSucceeded>();
    }

    [Fact]
    public async Task It_can_get_a_return_value()
    {
        using var kernel = await CreateKernelAsync();

        var result = await kernel.SendAsync(new SubmitCode("x = 123;\nreturn x;", "javascript"));

        var events = result.KernelEvents.ToSubscribedList();

        events.Should().ContainSingle<ReturnValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(v => v.MimeType == "application/json" &&
                                  v.Value == "123");
    }

    [Fact]
    public async Task It_can_get_console_log_output()
    {
        using var kernel = await CreateKernelAsync();

        var result = await kernel.SendAsync(new SubmitCode("console.log(123);", "javascript"));

        var events = result.KernelEvents.ToSubscribedList();

        events.Should().ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(v => v.MimeType == "text/plain" &&
                                  v.Value == "123");
    }

    private static async Task<CompositeKernel> CreateKernelAsync()
    {
        var compositeKernel = new CompositeKernel();

        var connector = new PlaywrightKernelConnector();

        var proxy = await connector.CreateKernelAsync("javascript");
        compositeKernel.Add(proxy, new[] { "js" });

        return compositeKernel;
    }
}