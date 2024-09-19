// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Pocket;

namespace Microsoft.DotNet.Interactive;

internal class ImmediateScheduler<T, TResult> : IKernelScheduler<T, TResult>
{
    private static readonly Logger Log = new("KernelScheduler (fast)");

    public async Task<TResult> RunAsync(
        T value, KernelSchedulerDelegate<T, TResult> onExecuteAsync,
        string scope = "default",
        CancellationToken cancellationToken = default)
    {
        using var logOp = Log.OnEnterAndConfirmOnExit(arg: value);

        var result = await onExecuteAsync(value);

        logOp.Succeed();

        return result;
    }
}