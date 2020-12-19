// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Interactive.AspNetCore
{
    internal class InteractiveLoggerProvider : ILoggerProvider
    {
        public event Action<LogMessage> Posted;

        public ILogger CreateLogger(string categoryName)
        {
            return new InteractiveLogger(this, categoryName);
        }

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
                var message = formatter(state, exception);

                Pocket.Logger.Log.Post(new Pocket.LogEntry(
                    logLevel: ToPocketLogLevel(logLevel),
                    message: message,
                    exception: exception,
                    category: _categoryName,
                    operationName: eventId.ToString()));

                _loggerProvider.Posted?.Invoke(new LogMessage
                {
                    LogLevel = logLevel,
                    Category = _categoryName,
                    EventId = eventId,
                    Message = message,
                    Exception = exception
                });
            }

            private static Pocket.LogLevel ToPocketLogLevel(LogLevel logLevel) =>
                logLevel switch
                {
                    LogLevel.Trace => Pocket.LogLevel.Trace,
                    LogLevel.Debug => Pocket.LogLevel.Debug,
                    LogLevel.Information => Pocket.LogLevel.Information,
                    LogLevel.Warning => Pocket.LogLevel.Warning,
                    LogLevel.Error => Pocket.LogLevel.Error,
                    LogLevel.Critical => Pocket.LogLevel.Critical,
                    _ => throw new NotSupportedException()
                };
        }
    }
}
