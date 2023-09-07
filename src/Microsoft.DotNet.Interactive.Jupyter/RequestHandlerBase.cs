// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Pocket;

namespace Microsoft.DotNet.Interactive.Jupyter;

public abstract class RequestHandlerBase<T> : IDisposable
    where T : RequestMessage
{
    private readonly CompositeDisposable _disposables = new();

    protected RequestHandlerBase(Kernel kernel, IScheduler scheduler)
    {
        Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        KernelEvents = Kernel.KernelEvents.ObserveOn(scheduler ?? throw new ArgumentNullException(nameof(scheduler)));
    }

    protected IObservable<KernelEvent> KernelEvents { get; }

    protected async Task SendAsync(
        JupyterRequestContext jupyterContext,
        KernelCommand command)
    {
        command.SetToken(jupyterContext.Token);

        using var sub = Kernel
            .KernelEvents
            .Subscribe(e =>
            {
                if (ShouldForward(e))
                {
                    try
                    {
                        OnKernelEventReceived(e, jupyterContext);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log.Error(ex);
                    }
                }
            });

        await Kernel.SendAsync(
            command,
            CancellationToken.None);

        bool ShouldForward(KernelEvent e)
        {
            return e.Command.GetOrCreateToken().StartsWith(jupyterContext.Token);
        }
    }

    protected abstract void OnKernelEventReceived(
        KernelEvent @event,
        JupyterRequestContext context);

    protected static T GetJupyterRequest(JupyterRequestContext context)
    {
        var request = context.GetRequestContent<T>() ??
                      throw new InvalidOperationException(
                          $"Request Content must be a not null {typeof(T).Name}");
        return request;
    }

    protected Kernel Kernel { get; }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}