// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.AspNetCore;

internal class EnhancedHttpClient
{
    private const string _logKey = "aspnetcore-logs";

    public static HttpClient Create(string address, InteractiveLoggerProvider interactiveLoggerProvider) =>
        new(new LogCapturingHandler(interactiveLoggerProvider))
        {
            BaseAddress = new Uri(address)
        };

    private class LogCapturingHandler : DelegatingHandler
    {
        private readonly InteractiveLoggerProvider _interactiveLoggerProvider;

        public LogCapturingHandler(InteractiveLoggerProvider interactiveLoggerProvider)
            : base(new SocketsHttpHandler())
        {
            _interactiveLoggerProvider = interactiveLoggerProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var logs = new ConcurrentQueue<LogMessage>();
            request.Options.Set(new HttpRequestOptionsKey<ConcurrentQueue<LogMessage>>(_logKey), logs);

            _interactiveLoggerProvider.Posted += logs.Enqueue;

            try
            {
                var responseMessage = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                // Wait to download the body so we catch all the logs. If someone wants to access the response before
                // downloading the body, they're free to create a their own HttpClient.
                await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

                return responseMessage;
            }
            finally
            {
                _interactiveLoggerProvider.Posted -= logs.Enqueue;
            }
        }
    }
}