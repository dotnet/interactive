// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive
{
    public static class KernelSchedulerExtensions
    {


        public static void RegisterDeferredOperationSource<T,U>(this KernelScheduler<T, U> kernelScheduler, KernelScheduler<T,U>.GetDeferredOperationsDelegate getDeferredOperations, Action<T> onExecute)
        {
            kernelScheduler.RegisterDeferredOperationSource(getDeferredOperations, v =>
            {
                onExecute(v);
                return Task.FromResult(default(U));
            });
        }

        internal static Task<KernelCommandResult> Schedule(this KernelScheduler<KernelCommand, KernelCommandResult> kernelScheduler, KernelCommand kernelCommand, Kernel kernel)
        {

            return kernelScheduler.Schedule(
                kernelCommand, async command =>
                {
                    var context = KernelInvocationContext.Establish(command);

                    // only subscribe for the root command 
                    using var _ =
                        context.Command == command
                            ? context.KernelEvents.Subscribe(kernel.PublishEvent)
                            : Disposable.Empty;

                    try
                    {
                        await kernel.Pipeline.SendAsync(command, context);

                        if (command == context.Command)
                        {
                            await context.DisposeAsync();
                        }
                        else
                        {
                            context.Complete(command);
                        }

                        return context.Result;
                    }
                    catch (Exception exception)
                    {
                        if (!context.IsComplete)
                        {
                            context.Fail(exception);
                        }

                        throw;
                    }
                }
                , kernelCommand.TargetKernelName);
        }
    }
}