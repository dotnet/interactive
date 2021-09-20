// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.Http
{
    public class KernelCommandAndEventSignalRHubConnectionReceiver : IKernelCommandAndEventReceiver, IDisposable
    {
        private readonly CompositeDisposable _disposables;
        private readonly Subject<string> _channel = new();
        private readonly KernelCommandAndEventObservableReceiver _internalReceiver;

        public KernelCommandAndEventSignalRHubConnectionReceiver(HubConnection receiver)
        {
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));
            {
                _internalReceiver = new KernelCommandAndEventObservableReceiver(_channel);
            }

            _disposables = new CompositeDisposable
            {   
                receiver.On<string>("kernelCommandFromRemote", e => _channel.OnNext(e)),
                receiver.On<string>("kernelEventFromRemote", e => _channel.OnNext(e)),
                _internalReceiver,
                //fix: remove this one as this is for backward compatibility
                receiver.On<string>("kernelEvent", e => _channel.OnNext(e)),
            };
        }
        public IAsyncEnumerable<CommandOrEvent> CommandsAndEventsAsync(CancellationToken cancellationToken)
        {
            return _internalReceiver.CommandsAndEventsAsync(cancellationToken);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}