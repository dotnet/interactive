// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class KernelCommandAndEventObservableReceiver : InteractiveProtocolKernelCommandAndEventReceiverBase, IDisposable
    {
        private readonly IObservable<string> _receiver;
        private readonly ConcurrentQueue<string> _queue;
        private readonly SemaphoreSlim _semaphore = new(0, 1);
        private readonly CompositeDisposable _disposables = new();

        public KernelCommandAndEventObservableReceiver(IObservable<string> receiver)
        {
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
            _queue = new ConcurrentQueue<string>();
            _disposables.Add(
                _receiver.Subscribe(message =>
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

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}