// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Pocket;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;
using Disposable = System.Reactive.Disposables.Disposable;

namespace Microsoft.DotNet.Interactive.Connection
{
    public sealed class MultiplexingKernelCommandAndEventReceiver : 
        IKernelCommandAndEventReceiver, 
        IDisposable
    {
        private static readonly Logger<MultiplexingKernelCommandAndEventReceiver> _log = new();
        private readonly IKernelCommandAndEventReceiver _source;
        private readonly CompositeDisposable _disposables = new();
        private ImmutableList<MultiplexedKernelCommandAndEventReceiver> _children = ImmutableList<MultiplexedKernelCommandAndEventReceiver>.Empty;
        
        public MultiplexingKernelCommandAndEventReceiver(IKernelCommandAndEventReceiver source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public async IAsyncEnumerable<CommandOrEvent> CommandsAndEventsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var commandOrEvent in _source.CommandsAndEventsAsync(cancellationToken))
            {
                var sources = _children.Select(c => c.ReceivedCommandsAndEvents).ToArray();

                if (sources.Length > 0)
                {
                    foreach (var destination in sources)
                    {
                        destination.Add(commandOrEvent, cancellationToken);
                    }
                }

                yield return commandOrEvent;
            }
        }

        public IKernelCommandAndEventReceiver CreateChildReceiver()
        {
            var receiver = new MultiplexedKernelCommandAndEventReceiver();
            
            _children = _children.Add(receiver);
            
            _disposables.Add( Disposable.Create(() =>
            {
                _children = _children.Remove(receiver);
                receiver.Dispose();
            }));
           
            return receiver;
        }

        private class MultiplexedKernelCommandAndEventReceiver : 
            IKernelCommandAndEventReceiver, 
            IDisposable
        {
            private static readonly Logger<MultiplexedKernelCommandAndEventReceiver> _log = new();

            private readonly CompositeDisposable _disposables = new();

            public MultiplexedKernelCommandAndEventReceiver()
            {
                _disposables.Add(Disposable.Create(() =>
                {
                    ReceivedCommandsAndEvents.CompleteAdding();
                }));
                _disposables.Add(ReceivedCommandsAndEvents);
            }

            public BlockingCollection<CommandOrEvent> ReceivedCommandsAndEvents { get; } = new();

            public void Dispose() => _disposables.Dispose();

            public async IAsyncEnumerable<CommandOrEvent> CommandsAndEventsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    using var op = _log.OnEnterAndExit();

                    await Task.Yield();

                    var commandOrEvent = ReceivedCommandsAndEvents.Take(cancellationToken);

                    op.Trace("ReceivedCommandsAndEvents.Take", commandOrEvent);

                    yield return commandOrEvent;
                }
            }
        }

        public void Dispose() => _disposables.Dispose();
    }
}