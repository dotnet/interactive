// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

[TestClass]
public class AsyncLazyTests
{
    [TestMethod]
    public async Task AsyncLazy_returns_the_specified_value()
    {
        var lazy = new AsyncLazy<string>(async () =>
        {
            await Task.Yield();

            return "hello!";
        });

        var value = await lazy.ValueAsync();

        value.Should().Be("hello!");
    }

    [TestMethod]
    public async Task AsyncLazy_can_be_awaited_concurrently_without_triggering_initialization_twice()
    {
        var callCount = 0;

        var barrier = new Barrier(5);

        var lazy = new AsyncLazy<string>(async () =>
        {
            await Task.Yield();

            barrier.SignalAndWait(2000);

            Interlocked.Increment(ref callCount);

            barrier.SignalAndWait(2000);

            return "hello!";
        });

        var calls = Enumerable.Range(1, 5)
            .Select(_ => lazy.ValueAsync());

        await Task.WhenAll(calls);

        callCount.Should().Be(1);
    }
}