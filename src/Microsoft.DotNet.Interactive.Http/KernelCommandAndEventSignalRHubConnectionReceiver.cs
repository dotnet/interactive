// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.Http
{
    public class KernelCommandAndEventSignalRHubConnectionReceiver : IKernelCommandAndEventReceiver, IDisposable
    {
        private readonly CompositeDisposable _disposables;
        private readonly Subject<string> _serializedCommandAndEventSubject = new();
        private readonly Receiver _internalReceiver;

        public KernelCommandAndEventSignalRHubConnectionReceiver(HubConnection hubConnection)
        {
            if (hubConnection == null)
            {
                throw new ArgumentNullException(nameof(hubConnection));
            }

            _internalReceiver = new Receiver(_serializedCommandAndEventSubject);

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

        public class Receiver :
            KernelCommandAndEventDeserializingReceiverBase,
            IDisposable
        {
            private readonly ConcurrentQueue<string> _queue;
            private readonly SemaphoreSlim _semaphore = new(0, 1);
            private readonly CompositeDisposable _disposables = new();

            public Receiver(IObservable<string> serializedCommandAndEventEnvelopes)
            {
                if (serializedCommandAndEventEnvelopes == null)
                {
                    throw new ArgumentNullException(nameof(serializedCommandAndEventEnvelopes));
                }

                _queue = new ConcurrentQueue<string>();
                _disposables.Add(
                    serializedCommandAndEventEnvelopes.Subscribe(message =>
                    {
                        _queue.Enqueue(message);
                        _semaphore.Release();
                    }));
                _disposables.Add(_semaphore);
            }

            protected override async Task<string> ReadMessageAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_queue.TryDequeue(out var message))
                {
                    return message;
                }

                await _semaphore.WaitAsync(cancellationToken);
                _queue.TryDequeue(out message);
                return message;
            }

            public void Dispose() => _disposables.Dispose();
        }
    }
}