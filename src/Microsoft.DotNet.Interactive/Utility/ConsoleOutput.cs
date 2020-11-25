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
        private TextWriter _originalOutputWriter;
        private TextWriter _originalErrorWriter;
        private static readonly ObservableStringWriter _outputWriter = new ObservableStringWriter();
        private static readonly ObservableStringWriter _errorWriter = new ObservableStringWriter();

        private const int NOT_DISPOSED = 0;
        private const int DISPOSED = 1;

        private int _alreadyDisposed = NOT_DISPOSED;

        private static readonly SemaphoreSlim _consoleLock = new SemaphoreSlim(1, 1);
        private static bool _isCaptured;
        private static RefCountDisposable _refCount;

        private ConsoleOutput()
        {
        }

        private static async Task<IObservableConsole> CaptureAsync()
        {
            if (_isCaptured)
            {
                return new ObservableConsole(
                    @out: _outputWriter,
                    error: _errorWriter);
            }

            var redirector = new ConsoleOutput();
            await _consoleLock.WaitAsync();

            try
            {
                redirector._originalOutputWriter = Console.Out;
                redirector._originalErrorWriter = Console.Error;

                Console.SetOut(_outputWriter);
                Console.SetError(_errorWriter);

                _isCaptured = true;
            }
            catch
            {
                _consoleLock.Release();
                throw;
            }

            return redirector;
        }

        public static async Task<IDisposable> TryCaptureAsync(Func<IObservableConsole, IDisposable> onCaptured)
        {
            if (!_isCaptured)
            {
                var console = await CaptureAsync();

                var disposables = onCaptured(console);

                return new CompositeDisposable
                {
                    disposables,
                    console
                };
            }

            return Task.CompletedTask;
        }

        public static async Task<IDisposable> SubscribeAsync(Func<IObservableConsole, IDisposable> subscribe)
        {
            await _consoleLock.WaitAsync();

            using var _ = Disposable.Create(() => _consoleLock.Release());

            if (Console.Out is not MultiplexingTextWriter @out ||
                Console.Error is not MultiplexingTextWriter error)
            {
                var console = new ConsoleOutput
                {
                    _originalOutputWriter = Console.Out,
                    _originalErrorWriter = Console.Error
                };

                @out = new MultiplexingTextWriter();
                error = new MultiplexingTextWriter();

                EnsureInitializedForCurrentAsyncContext();

                Console.SetOut(@out);
                Console.SetError(error);

                _isCaptured = true;

                _refCount = new RefCountDisposable(Disposable.Create(() =>
                {
                    _refCount = null;

                    console.RestoreSystemConsole();
                }));

                return new CompositeDisposable {
                    _refCount,
                    subscribe(console)
                };
            }
            else
            {
                EnsureInitializedForCurrentAsyncContext();

                var console = new ObservableConsole(
                    @out: @out.GetObservable(),
                    error: error.GetObservable());

                return new CompositeDisposable{
                    _refCount.GetDisposable(),
                    subscribe(console)};
            }

            void EnsureInitializedForCurrentAsyncContext()
            {
                @out.EnsureInitializedForCurrentAsyncContext();
                error.EnsureInitializedForCurrentAsyncContext();
            }
        }

        public IObservable<string> Out => _outputWriter;

        public IObservable<string> Error => _errorWriter;

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

                _isCaptured = false;
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