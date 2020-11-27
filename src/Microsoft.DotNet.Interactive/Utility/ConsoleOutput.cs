// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Utility
{
    internal class ConsoleOutput : IObservableConsole
    {
        private static readonly SemaphoreSlim _consoleLock = new SemaphoreSlim(1, 1);
        private static RefCountDisposable _refCount;
        private static MultiplexingTextWriter _out;
        private static MultiplexingTextWriter _error;

        private TextWriter _originalOutputWriter;
        private TextWriter _originalErrorWriter;

        private const int NOT_DISPOSED = 0;
        private const int DISPOSED = 1;

        private int _alreadyDisposed = NOT_DISPOSED;

        private ConsoleOutput()
        {
        }

        public static async Task<IDisposable> SubscribeAsync(Func<IObservableConsole, IDisposable> subscribe)
        {
            await _consoleLock.WaitAsync();

            using var _ = Disposable.Create(() => _consoleLock.Release());

            if (_refCount is null)
            {
                var console = new ConsoleOutput
                {
                    _originalOutputWriter = Console.Out,
                    _originalErrorWriter = Console.Error
                };

                _out = new MultiplexingTextWriter();
                _error = new MultiplexingTextWriter();

                EnsureInitializedForCurrentAsyncContext();

                Console.SetOut(_out);
                Console.SetError(_error);

                _refCount = new RefCountDisposable(Disposable.Create(() =>
                {
                    _out = null;
                    _error = null;
                    _refCount = null;

                    console.RestoreSystemConsole();
                }));

                var subscription = subscribe(console);

                return new CompositeDisposable {
                    _refCount,
                    subscription
                };
            }
            else if (AsyncContext.Id is not null)
            {
                return Disposable.Empty;
            }
            else
            {
                EnsureInitializedForCurrentAsyncContext();

                var console = new ObservableConsole(
                    @out: _out.GetObservable(),
                    error: _error.GetObservable());

                return new CompositeDisposable{
                    _refCount.GetDisposable(),
                    subscribe(console)
                };
            }


            void EnsureInitializedForCurrentAsyncContext()
            {
                _out.EnsureInitializedForCurrentAsyncContext();
                _error.EnsureInitializedForCurrentAsyncContext();
            }
            }

        public IObservable<string> Out => _out.GetObservable();

        public IObservable<string> Error => _error.GetObservable();

        public void Dispose()
        {
            RestoreSystemConsole();
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

        private class ObservableConsole : IObservableConsole
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

            public void Dispose()
            {
            }
        }
    }
}