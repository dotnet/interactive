// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.Http
{
    internal class SignalRInputTextStream : IInputTextStream
    {
        private readonly Subject<string> _channel = new();
        private readonly HubConnection _connection;
        private readonly CompositeDisposable _disposables;

        public SignalRInputTextStream(HubConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _disposables = new CompositeDisposable
            {
                _connection.On<string>("kernelEvent", e => _channel.OnNext(e))
            };
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            return _channel.Subscribe(observer);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        public bool IsStarted => _connection.State == HubConnectionState.Connected;

    }

    public class KernelCommandAndEventObservableReceiver : KernelCommandAndEventReceiverBase
    {
        private readonly IObservable<string> _receiver;
        private readonly ConcurrentQueue<string> _queue;


        public KernelCommandAndEventObservableReceiver(IObservable<string> receiver)
        {
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
            _queue = new ConcurrentQueue<string>();
            _receiver.Subscribe(message =>
            {
                _queue.Enqueue(message);
            });
        }

        protected override async Task<string> ReadMessageAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_queue.TryDequeue(out var message))
            {
                return message;
            }
            await _receiver.FirstAsync();
            _queue.TryDequeue(out message);
            return message;
        }
    }
}