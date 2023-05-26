// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

public class ConsoleOutputTests
{
    [Fact]
    public void Console_output_is_captured_for_the_current_async_context()
    {
        var output = new StringBuilder();

        using var subscription = ConsoleOutput.Subscribe(c => c.Out.Subscribe(s => output.Append(s)));

        Console.WriteLine("hello");

        output.ToString().Should().Contain("hello");
    }

    [Fact]
    public async Task Console_output_is_not_captured_from_other_async_contexts()
    {
        var output = new StringBuilder();

        using var subscription = ConsoleOutput.Subscribe(c => c.Out.Subscribe(s => output.Append(s)));

        ExecutionContext.SuppressFlow();

        await Task.Run(() => Console.WriteLine("hello"));

        output.ToString().Should().NotContain("hello");
    }

    [Fact]
    public async Task Console_output_can_be_captured_from_other_async_contexts_for_known_async_context_id()
    {
        var output = new StringBuilder();

        using var subscription = ConsoleOutput.Subscribe(c => c.Out.Subscribe(s => output.Append(s)));

        var asyncContextId = AsyncContext.Id;

        Console.Write("one");

        ExecutionContext.SuppressFlow();

        await Task.Run(() =>
        {
            ConsoleOutput.InitializeFromAsyncContext(asyncContextId.Value);

            Console.WriteLine("two");
        });

        output.ToString().Should().ContainAll("one", "two");
    }
}