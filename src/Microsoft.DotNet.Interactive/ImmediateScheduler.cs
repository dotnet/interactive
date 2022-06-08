// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    internal class ImmediateScheduler<T, TResult> : IKernelScheduler<T, TResult>
    {
        public async Task<TResult> RunAsync(
            T value, KernelSchedulerDelegate<T, TResult> onExecuteAsync,
            string scope = "default",
            CancellationToken cancellationToken = default)
        {
            return await onExecuteAsync(value);
        }
    }
}