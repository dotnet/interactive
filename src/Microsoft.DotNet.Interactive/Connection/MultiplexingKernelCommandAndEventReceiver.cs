// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Connection
{
    public class KernelConnectionManager: IDisposable
    {
        private readonly ConcurrentDictionary<string, (IKernelCommandAndEventSender sender, MultiplexingKernelCommandAndEventReceiver multiplexingReceiver)> _storage = new();

        public bool TryGetConnection(string connectionId, out IKernelCommandAndEventSender sender,
            out MultiplexingKernelCommandAndEventReceiver multiplexingReceiver)
        {
            var found = _storage.TryGetValue(connectionId, out var connection);
            sender = found ? connection.sender : null;
            multiplexingReceiver = found ? connection.multiplexingReceiver : null;
            return found;
        }

        public void AddConnection(string connectionId, IKernelCommandAndEventSender sender,
            MultiplexingKernelCommandAndEventReceiver multiplexingReceiver)
        {
            _storage[connectionId] = new (sender, multiplexingReceiver);
        }

        public void Dispose()
        {
            foreach (var connection in _storage)
            {
                connection.Value.multiplexingReceiver.Dispose();
            }
        }
    }

    public class MultiplexingKernelCommandAndEventReceiver : IKernelCommandAndEventReceiver, IDisposable
    {
        private readonly IKernelCommandAndEventReceiver _source;
        private readonly CompositeDisposable _disposables = new();
        private readonly List<MultiplexedKernelCommandAndEventReceiver> _children = new();

        public MultiplexingKernelCommandAndEventReceiver(IKernelCommandAndEventReceiver source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public async IAsyncEnumerable<CommandOrEvent> CommandsAndEventsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var commandOrEvent in _source.CommandsAndEventsAsync(cancellationToken))
            {
                if (_children.Count > 0)
                {
                    BlockingCollection<CommandOrEvent>.AddToAny(_children.Select(c => c.LocalStorage).ToArray(), commandOrEvent, cancellationToken);
                }
                yield return commandOrEvent;
            }
        }

        public IKernelCommandAndEventReceiver CreateChildReceiver()
        {
            var receiver = new MultiplexedKernelCommandAndEventReceiver();
            _children.Add(receiver);
            _disposables.Add( Disposable.Create(() =>
            {
                _children.Remove(receiver);
                receiver.Dispose();
            }));
           
            return receiver;
        }

        private class MultiplexedKernelCommandAndEventReceiver : IKernelCommandAndEventReceiver, IDisposable
        {
            public BlockingCollection<CommandOrEvent> LocalStorage { get; } = new();
            private readonly CompositeDisposable _disposables = new();
            public MultiplexedKernelCommandAndEventReceiver()
            {
                _disposables.Add(Disposable.Create(() =>
                {
                    LocalStorage.CompleteAdding();
                }));
                _disposables.Add(LocalStorage);
            }

            public void Dispose()
            {
                _disposables.Dispose();
            }

            public async IAsyncEnumerable<CommandOrEvent> CommandsAndEventsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var commandOrEvent = LocalStorage.Take(cancellationToken);
                    await Task.Yield();
                    yield return commandOrEvent;
                }
            }
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }
    }
}