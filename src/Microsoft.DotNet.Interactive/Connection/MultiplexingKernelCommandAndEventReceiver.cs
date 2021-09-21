// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class MultiplexingKernelCommandAndEventReceiver : IKernelCommandAndEventReceiver, IDisposable
    {
        private readonly IKernelCommandAndEventReceiver _source;
        private readonly Subject<CommandOrEvent> _internalChannel = new();
        private readonly CompositeDisposable _disposables = new();

        public MultiplexingKernelCommandAndEventReceiver(IKernelCommandAndEventReceiver source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public async IAsyncEnumerable<CommandOrEvent> CommandsAndEventsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var commandOrEvent in _source.CommandsAndEventsAsync(cancellationToken))
            {
                _internalChannel.OnNext(commandOrEvent);
                yield return commandOrEvent;
            }

            _internalChannel.OnCompleted();
        }

        public IKernelCommandAndEventReceiver GetReceiver()
        {
            var receiver = new MultiplexedKernelCommandAndEventReceiver(_internalChannel);
            _disposables.Add(receiver);
           
            return receiver;
        }

        private class MultiplexedKernelCommandAndEventReceiver : IKernelCommandAndEventReceiver, IDisposable
        {
            private readonly ConcurrentQueue<CommandOrEvent> _queue;
            private readonly SemaphoreSlim _semaphore = new(0, 1);
            private readonly CompositeDisposable _disposables = new();

            public MultiplexedKernelCommandAndEventReceiver(IObservable<CommandOrEvent> receiver)
            {
                receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
                _queue = new ConcurrentQueue<CommandOrEvent>();
                _disposables.Add(
                    receiver.Subscribe(message =>
                    {
                        _queue.Enqueue(message);
                        _semaphore.Release();
                    }));
                _disposables.Add(_semaphore);
            }

            public void Dispose()
            {
                _disposables.Dispose();
            }

            public async IAsyncEnumerable<CommandOrEvent> CommandsAndEventsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_queue.TryDequeue(out var message))
                    {
                        yield return message;
                    }
                    await _semaphore.WaitAsync(cancellationToken);
                    _queue.TryDequeue(out message);
                   yield return message;
                }
            }
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}