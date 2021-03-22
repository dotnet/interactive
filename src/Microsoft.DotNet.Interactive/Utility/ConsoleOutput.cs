// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Pocket;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;
using Disposable = System.Reactive.Disposables.Disposable;

namespace Microsoft.DotNet.Interactive.Utility
{
    internal class ConsoleOutput
    {
        private static readonly object _systemConsoleSwapLock = new();

        private static MultiplexingTextWriter _multiplexingOutputWriter;
        private static MultiplexingTextWriter _multiplexingErrorWriter;
        private static TextWriter _originalOutputWriter;
        private static TextWriter _originalErrorWriter;

        private static int _refCount = 0;

        private static readonly Logger Log = new(nameof(ConsoleOutput));

        private static OperationLogger _operationLogger;

        private ConsoleOutput()
        {
        }

        public static IDisposable Subscribe(Func<ObservableConsole, IDisposable> subscribe)
        {
            lock (_systemConsoleSwapLock)
            {
                if (++_refCount == 1)
                {
                    _operationLogger = Log.OnEnterAndExit(
                        $"Console swap on AsyncContext.Id {AsyncContext.Id?.ToString() ?? "none"}",
                        exitArgs: () => new[] { ("AsyncContext.Id", (object) AsyncContext.Id) });
                    _originalOutputWriter = Console.Out;
                    _originalErrorWriter = Console.Error;
                    _multiplexingOutputWriter = new MultiplexingTextWriter("out");
                    _multiplexingErrorWriter = new MultiplexingTextWriter("err");
                    Console.SetOut(_multiplexingOutputWriter);
                    Console.SetError(_multiplexingErrorWriter);
                }
            }

            _multiplexingOutputWriter.EnsureInitializedForCurrentAsyncContext();
            _multiplexingErrorWriter.EnsureInitializedForCurrentAsyncContext();

            var obsConsole = new ObservableConsole(
                _multiplexingOutputWriter.GetObservable(),
                _multiplexingErrorWriter.GetObservable());

            return new CompositeDisposable(
                subscribe(obsConsole),
                Disposable.Create(() =>
                {
                    lock (_systemConsoleSwapLock)
                    {
                        if (--_refCount == 0)
                        {
                            Console.SetOut(_originalOutputWriter);
                            Console.SetError(_originalErrorWriter);
                        }
                    }
                }),
                _operationLogger);
        }

        internal record ObservableConsole(
            IObservable<string> Out,
            IObservable<string> Error);
    }
}