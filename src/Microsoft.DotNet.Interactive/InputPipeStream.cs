// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Pipes;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    internal class InputPipeStream : IObservable<string>, IDisposable
    {
        private readonly object _lock = new object();
        private readonly PipeStream _input;
        private readonly Subject<string> _channel = new Subject<string>();
        private bool _complete;

        public InputPipeStream(PipeStream input)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
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
                    var message = await _input.ReadMessageAsync();
                    if (string.IsNullOrEmpty(message))
                    {
                        await Task.Delay(100);
                    }
                    else
                    {
                        _channel.OnNext(message);
                    }
                }
            });
        }

        public void Dispose()
        {
            _channel.OnCompleted();
            _complete = true;
        }

        public bool IsStarted { get; private set; }
    }
}