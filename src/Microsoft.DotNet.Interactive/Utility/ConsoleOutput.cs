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

        private static async Task<IObservableConsole> CaptureAsync2()
        {
            await _consoleLock.WaitAsync();

            using var _ = Disposable.Create(() => _consoleLock.Release());

            if (!(Console.Out is MultiplexingTextWriter @out) ||
                !(Console.Error is MultiplexingTextWriter error))
            {
                var multiplexed = new ConsoleOutput
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

                return multiplexed;
            }
            else
            {
                EnsureInitializedForCurrentAsyncContext();
                return new ObservableConsole(
                    @out: @out.GetObservable(),
                    error: error.GetObservable());
            }

            void EnsureInitializedForCurrentAsyncContext()
            {
                @out.EnsureInitializedForCurrentAsyncContext();
                error.EnsureInitializedForCurrentAsyncContext();
            }
        }

        public static async Task<IDisposable> TryCaptureAsync2(Func<IObservableConsole, IDisposable> onCaptured)
        {
            if (!_isCaptured)
            {
                var console = await CaptureAsync2();

                var disposables = onCaptured(console);

                return new CompositeDisposable
                {
                    disposables,
                    console
                };
            }

            return Task.CompletedTask;
        }

        public IObservable<string> Out => _outputWriter;

        public IObservable<string> Error => _errorWriter;
      
        public void Dispose()
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

                _consoleLock.Release();
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