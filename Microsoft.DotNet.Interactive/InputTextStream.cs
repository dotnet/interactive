// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    internal class InputTextStream : IObservableStream, IDisposable
    {
        private readonly object _lock = new object();
        private readonly TextReader _input;
        private readonly Subject<string> _channel = new Subject<string>();
        private bool _complete;
        private bool _started;

        public InputTextStream(TextReader input)
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
                if (_started)
                {
                    return;
                }

                _started = true;
            }

            Task.Run(async () =>
            {
                while (!_complete)
                {
                    var line = await _input.ReadLineAsync();
                    if (line == null)
                    {
                        await Task.Delay(100);
                    }
                    else
                    {
                        _channel.OnNext(line);
                    }
                }
            });
        }

        public void Dispose()
        {
            _channel.OnCompleted();
            _complete = true;
        }

        public bool IsStarted => _started;
    }
}