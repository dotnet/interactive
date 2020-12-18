// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Interactive.AspNetCore
{
    internal class InteractiveLoggerProvider : ILoggerProvider
    {
        public static event Action<LogMessage> Posted;

        public ILogger CreateLogger(string categoryName)
        {
            return new InteractiveLogger(categoryName);
        }

        public void Dispose()
        {
        }

        private class InteractiveLogger : ILogger, IDisposable
        {
            private readonly string _categoryName;

            public InteractiveLogger(string categoryName)
            {
                _categoryName = categoryName;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                // this.Dispose() no-ops
                return this;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                var message = formatter(state, exception);
                Pocket.LoggerExtensions.Info(Pocket.Logger.Log, message);
                Posted?.Invoke(new LogMessage
                {
                    LogLevel = logLevel,
                    Category = _categoryName,
                    EventId = eventId,
                    Message = message,
                    Exception = exception
                });
            }

            public void Dispose()
            {
            }
        }
    }
}
