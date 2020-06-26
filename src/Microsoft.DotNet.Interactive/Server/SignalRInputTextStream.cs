// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Microsoft.AspNetCore.SignalR.Client;

namespace Microsoft.DotNet.Interactive.Server
{
    internal class SignalRInputTextStream : IInputTextStream
    {
        private readonly Subject<string> _channel = new Subject<string>();
        private readonly HubConnection _connection;
        private readonly CompositeDisposable _disposables;
        public SignalRInputTextStream(HubConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _disposables = new CompositeDisposable
            {
               _connection.On<string>("kernelEvent", e=> _channel.OnNext(e) )
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
}