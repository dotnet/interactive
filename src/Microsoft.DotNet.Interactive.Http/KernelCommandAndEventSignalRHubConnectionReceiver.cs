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
        private readonly Subject<string> _serializedCommandAndEventSubject = new();
        private readonly SerializedCommandAndEventReceiver _internalReceiver;

        public KernelCommandAndEventSignalRHubConnectionReceiver(HubConnection hubConnection)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            _internalReceiver = new SerializedCommandAndEventReceiver(_serializedCommandAndEventSubject);

            _disposables = new CompositeDisposable
            {   
                hubConnection.On<string>("kernelCommandFromRemote", e => _serializedCommandAndEventSubject.OnNext(e)),
                hubConnection.On<string>("kernelEventFromRemote", e => _serializedCommandAndEventSubject.OnNext(e)),
                _internalReceiver,
                //fix: remove this one as this is for backward compatibility
                hubConnection.On<string>("kernelEvent", e => _serializedCommandAndEventSubject.OnNext(e)),
            };
        }
        public IAsyncEnumerable<CommandOrEvent> CommandsAndEventsAsync(CancellationToken cancellationToken)
        {
            return _internalReceiver.CommandsAndEventsAsync(cancellationToken);
        }

        public void Dispose() => _disposables.Dispose();
    }
}