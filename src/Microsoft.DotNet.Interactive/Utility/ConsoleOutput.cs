// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;

namespace Microsoft.DotNet.Interactive.Utility
{
    internal class ConsoleOutput
    {
        private static RefCountDisposable _refCount;
        private static MultiplexingTextWriter _out;
        private static MultiplexingTextWriter _error;
        private static readonly object _systemConsoleSwapLock = new object();

        private TextWriter _originalOutputWriter;
        private TextWriter _originalErrorWriter;

        private const int NOT_DISPOSED = 0;
        private const int DISPOSED = 1;

        private int _alreadyDisposed = NOT_DISPOSED;

        private ConsoleOutput()
        {
        }

        public static IDisposable Subscribe(Func<ObservableConsole, IDisposable> subscribe)
        {
            lock (_systemConsoleSwapLock)
            {
                if (_refCount is null || _refCount.IsDisposed)
                {
                    var console = new ConsoleOutput
                    {
                        _originalOutputWriter = Console.Out,
                        _originalErrorWriter = Console.Error
                    };

                    _out = new MultiplexingTextWriter();
                    _error = new MultiplexingTextWriter();

                    Console.SetOut(_out);
                    Console.SetError(_error);

                    _refCount = new RefCountDisposable(Disposable.Create(() =>
                    {
                        _out = null;
                        _error = null;
                        _refCount = null;

                        console.RestoreSystemConsole();
                    }));
                }

                var writerForCurrentContext = EnsureInitializedForCurrentAsyncContext();

                var observableConsole = new ObservableConsole(
                    @out: _out.GetObservable(),
                    error: _error.GetObservable());

                return new CompositeDisposable
                {
                    _refCount,
                    _refCount.GetDisposable(),
                    subscribe(observableConsole),
                    writerForCurrentContext
                };

                IDisposable EnsureInitializedForCurrentAsyncContext() =>
                    new CompositeDisposable
                    {
                        _out.EnsureInitializedForCurrentAsyncContext(),
                        _error.EnsureInitializedForCurrentAsyncContext()
                    };
            }
        }

        private void RestoreSystemConsole()
        {
            if (Interlocked.CompareExchange(ref _alreadyDisposed, DISPOSED, NOT_DISPOSED) == NOT_DISPOSED)
            {
                if (_originalOutputWriter != null)
                {
                    Console.SetOut(_originalOutputWriter);
                }

                if (_originalErrorWriter != null)
                {
                    Console.SetError(_originalErrorWriter);
                }
            }
        }

        internal class ObservableConsole
        {
            public ObservableConsole(
                IObservable<string> @out,
                IObservable<string> error)
            {
                Out = @out;
                Error = error;
            }

            public IObservable<string> Out { get; }
            public IObservable<string> Error { get; }
        }
    }
}