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

                Pocket.LoggerExtensions.Info(Pocket.Logger.Log, message);

                _loggerProvider.Posted?.Invoke(new LogMessage
                {
                    LogLevel = logLevel,
                    Category = _categoryName,
                    EventId = eventId,
                    Message = message,
                    Exception = exception
                });
            }
        }
    }
}
