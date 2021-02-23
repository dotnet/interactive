// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public static class KernelSchedulerExtensions
    {
        public static Task<U> Schedule<T,U>(this KernelScheduler<T,U> kernelScheduler, T value, Action<T> onExecute, string scope = "default")
        {
            return kernelScheduler.Schedule(value, v =>
            {
                onExecute(v);
                return Task.CompletedTask;
            },scope);
        }

        public static void RegisterDeferredOperationSource<T,U>(this KernelScheduler<T, U> kernelScheduler, KernelScheduler<T,U>.GetDeferredOperationsDelegate getDeferredOperations, Action<T> onExecute)
        {
            kernelScheduler.RegisterDeferredOperationSource(getDeferredOperations, v =>
            {
                onExecute(v);
                return Task.CompletedTask;
            });
        }
    }
}