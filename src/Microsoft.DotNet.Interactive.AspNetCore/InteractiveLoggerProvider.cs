// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Interactive.AspNetCore
{
    internal class InteractiveLoggerProvider : ILoggerProvider
    {
        private volatile ExecutionContext _pocketLoggerEC;

        public event Action<LogMessage> Posted;

        public ILogger CreateLogger(string categoryName)
        {
            return new InteractiveLogger(this, categoryName);
        }

        public IDisposable SubscribePocketLogerWithCurrentEC() => new PocketLoggerSubscription(this);

        public void Dispose()
        {
        }

        private class InteractiveLogger : ILogger
        {
            private readonly InteractiveLoggerProvider _loggerProvider;
            private readonly string _categoryName;

            public InteractiveLogger(InteractiveLoggerProvider loggerProvider, string categoryName)
            {
                _loggerProvider = loggerProvider;
                _categoryName = categoryName;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                // InteractiveLoggerProvider.Dispose() no-ops
                return _loggerProvider;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                var logMessage = new LogMessage
                {
                    LogLevel = logLevel,
                    Category = _categoryName,
                    EventId = eventId,
                    Message = formatter(state, exception),
                    Exception = exception
                };

                _loggerProvider.Posted?.Invoke(logMessage);
                PostPocketLog(logMessage);
            }

            private void PostPocketLog(LogMessage logMessage)
            {
                static void PocketLogCallback(object state)
                {
                    var logMessage = (LogMessage)state;

                    static Pocket.LogLevel ToPocketLogLevel(LogLevel logLevel) => logLevel switch
                    {
                        LogLevel.Trace => Pocket.LogLevel.Trace,
                        LogLevel.Debug => Pocket.LogLevel.Debug,
                        LogLevel.Information => Pocket.LogLevel.Information,
                        LogLevel.Warning => Pocket.LogLevel.Warning,
                        LogLevel.Error => Pocket.LogLevel.Error,
                        LogLevel.Critical => Pocket.LogLevel.Critical,
                        _ => throw new NotSupportedException()
                    };

                    Pocket.Logger.Log.Post(new Pocket.LogEntry(
                        logLevel: ToPocketLogLevel(logMessage.LogLevel),
                        message: logMessage.Message,
                        exception: logMessage.Exception,
                        category: logMessage.Category,
                        operationName: logMessage.EventId.ToString()));
                }

                if (_loggerProvider._pocketLoggerEC is {} currentEc)
                {
                    ExecutionContext.Run(currentEc, PocketLogCallback, logMessage);
                }
                else
                {
                    PocketLogCallback(logMessage);
                }
            }
        }

        private class PocketLoggerSubscription : IDisposable
        {
            private readonly InteractiveLoggerProvider _loggerProvider;
            private readonly ExecutionContext _previousEC;

            // This is only used to assert that loggerProvider._pocketLoggerEC hasn't changed in Dispose.
            private readonly ExecutionContext _currentEC;

            public PocketLoggerSubscription(InteractiveLoggerProvider loggerProvider)
            {
                _loggerProvider = loggerProvider;
                _previousEC = loggerProvider._pocketLoggerEC;
                _currentEC = ExecutionContext.Capture();

                loggerProvider._pocketLoggerEC = _currentEC;
            }

            public void Dispose()
            {
                Debug.Assert(ReferenceEquals(_loggerProvider._pocketLoggerEC, _currentEC),
                    "SubscribePocketLogerWithCurrentEC() should never be called concurrently.");

                _loggerProvider._pocketLoggerEC = _previousEC;
            }
        }
    }
}
