// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharpProject.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public class ConsoleRedirectionTests
{
    [Fact]
    public async Task StandardOutput_is_captured()
    {
        using (var console = await ConsoleOutput.Capture())
        {
            Console.Write("hello");

            console.StandardOutput.Should().Be("hello");
        }
    }

    [Fact]
    public async Task StandardError_is_captured()
    {
        using (var console = await ConsoleOutput.Capture())
        {
            var message = $"oops! from {nameof(StandardError_is_captured)}";

            Console.Error.Write(message);

            console.StandardError.Should().Be(message);
        }
    }

    [Fact]
    public async void Multiple_threads_each_capturing_console_dont_conflict()
    {
        const int PRINT_COUNT = 10;
        const int THREAD_COUNT = 10;
        var barrier = new Barrier(THREAD_COUNT);

        async Task ThreadWork(string toPrint)
        {
            barrier.SignalAndWait(1000 /*ms*/);
            using (var console = await ConsoleOutput.Capture())
            {
                var builder = new StringBuilder();
                for (var i = 0; i < PRINT_COUNT; i++)
                {
                    Console.Write(toPrint);
                    builder.Append(toPrint);
                    await Task.Yield();
                }

                console.StandardOutput.Should().Be(builder.ToString());
            }
        }

        var threads = new List<Task>();
        for (var i = 0; i < THREAD_COUNT; i++)
        {
            threads.Add(Task.Run(async () => await ThreadWork($"hello from thread {i}!")));
        }

        foreach (var thread in threads)
        {
            await thread;
        }
    }
}