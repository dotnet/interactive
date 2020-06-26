// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Server
{
    public abstract class InputTextStream : IInputTextStream
    {
        private readonly object _lock = new object();
       
        private readonly Subject<string> _channel = new Subject<string>();
        private bool _complete;
        private readonly CancellationTokenSource _cancellationSource;


        protected InputTextStream()
        {
            _cancellationSource = new CancellationTokenSource();
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            EnsureStarted();

            return new CompositeDisposable
            {
                Disposable.Create(() => _complete = true),
                _channel.Subscribe(observer)
            };
        }

        protected abstract Task<string> ReadLineAsync();

        private void EnsureStarted()
        {
            lock (_lock)
            {
                if (IsStarted)
                {
                    return;
                }

                IsStarted = true;
            }

            Task.Run(async () =>
            {
                while (!_complete)
                {
                    var line = await ReadLineAsync();
                    if (line == null)
                    {
                        await Task.Delay(100, _cancellationSource.Token);
                    }
                    else
                    {
                        _channel.OnNext(line);
                    }
                }
            }, _cancellationSource.Token);
        }

        public void Dispose()
        {
            _channel.OnNext(string.Empty);
            _channel.OnCompleted();
            _complete = true;
            _cancellationSource.Cancel(false);
        }

        public bool IsStarted { get; private set; }
    }
}