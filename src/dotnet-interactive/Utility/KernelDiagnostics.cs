// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Pocket;

namespace Microsoft.DotNet.Interactive;

internal static class KernelDiagnostics
{
    [DebuggerStepThrough]
    public static T LogCommandsToPocketLogger<T>(this T kernel)
        where T : Kernel
    {
        kernel.AddMiddleware(async (command, context, next) =>
        {
            using var _ = Logger.Log.OnEnterAndExit();
            Logger.Log.Info(command);

            await next(command, context);
        });
        return kernel;
    }

    [DebuggerStepThrough]
    public static T LogEventsToPocketLogger<T>(this T kernel)
        where T : Kernel
    {
        var disposables = new CompositeDisposable();

        disposables.Add(
            kernel.KernelEvents.Subscribe(e =>
            {
                Logger.Log.Info("{kernel}: {event}",
                                kernel.Name,
                                e);
            }));

        kernel.VisitSubkernels(k =>
        {
            disposables.Add(
                k.KernelEvents.Subscribe(e =>
                {
                    Logger.Log.Info("{kernel}: {event}",
                                    k.Name,
                                    e);
                }));
        });

        kernel.RegisterForDisposal(disposables);

        return kernel;
    }
}