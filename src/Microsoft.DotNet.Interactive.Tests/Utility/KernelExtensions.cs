// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Pocket;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

public static class KernelExtensions
{
    public static async Task<(bool success, ValueInfosProduced valueInfosProduced)> TryRequestValueInfosAsync(this Kernel kernel)
    {
        if (kernel.SupportsCommandType(typeof(RequestValueInfos)))
        {
            var result = await kernel.SendAsync(new RequestValueInfos());

            var candidateResult = await result.KernelEvents.OfType<ValueInfosProduced>().FirstOrDefaultAsync();
            if (candidateResult is { })
            {
                return (true, candidateResult);
            }
        }

        return (false, default);
    }

    public static async Task<(bool success, ValueProduced valueProduced)> TryRequestValueAsync(this Kernel kernel, string valueName)
    {
        if (kernel.SupportsCommandType(typeof(RequestValue)))
        {
            var commandResult = await kernel.SendAsync(new RequestValue(valueName));

            if (await commandResult.KernelEvents.OfType<ValueProduced>().FirstOrDefaultAsync() is { } valueProduced)
            {
                return (true, valueProduced);
            }
        }

        return (false, default);
    }


    [DebuggerStepThrough]
    public static T LogCommandsToPocketLogger<T>(this T kernel)
        where T : Kernel
    {
        kernel.AddMiddleware(async (command, context, next) =>
        {
            using var _ = Logger.Log.OnEnterAndExit($"Command: {command.ToString().Replace(Environment.NewLine, " ")}");

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
            kernel.KernelEvents
                  .Subscribe(
                      e =>
                      {
                          Logger.Log.Info("{kernel}: {event}",
                                          kernel.Name,
                                          e);
                      }));

        kernel.VisitSubkernels(k =>
        {
            disposables.Add(
                k.KernelEvents.Subscribe(
                    e =>
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