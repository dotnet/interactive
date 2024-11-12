// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions.Execution;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

[DebuggerStepThrough]
public static class Wait
{
    public static void Until(
        Func<bool> condition,
        int timeoutMs = 5000)
    {
        var cancellationToken = new CancellationTokenSource(timeoutMs);

        while (!cancellationToken.Token.IsCancellationRequested)
        {
            if (condition())
            {
                return;
            }

            Thread.Sleep(50);
        }

        throw new AssertionFailedException($"Failed after waiting {timeoutMs}ms");
    }

    public static async Task Timeout(
        this Task source,
        TimeSpan timeout)
    {
        if (await Task.WhenAny(
                source,
                Task.Delay(timeout)) != source)
        {
            throw new TimeoutException();
        }

        await source;
    }
}