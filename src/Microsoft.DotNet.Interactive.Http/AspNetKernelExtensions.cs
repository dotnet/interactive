// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Routing;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Http
{
    public static class AspNetKernelExtensions
    {
        private const string _aspnetLogsKey = "aspnet-logs";
        private const string _prelude = @"
private static int __AspNet_NextEndpointOrder;

static IEndpointConventionBuilder MapAction(
    this IEndpointRouteBuilder endpoints,
    string pattern,
    RequestDelegate requestDelegate)
{
    var order = __AspNet_NextEndpointOrder--;
    var builder = endpoints.MapGet(pattern, requestDelegate);
    builder.Add(b => ((RouteEndpointBuilder)b).Order = order);
    return builder;
}";

        public static T UseAspNet<T>(this T kernel)
            where T : DotNetKernel
        {
            // TODO: Manage lifetime. Allow stopping, restarting, selecting port/in-memory etc...
            var isActive = false;

            var directive = new Command("#!aspnet", "Activate ASP.NET")
            {
                Handler = CommandHandler.Create<string, string, KernelInvocationContext>(async (from, name, context) =>
                {
                    // REVIEW: Commands cannot run concurrently, right?
                    if (isActive)
                    {
                        return;
                    }

                    isActive = true;

                    Environment.SetEnvironmentVariable($"ASPNETCORE_{WebHostDefaults.PreventHostingStartupKey}", "true");

                    IApplicationBuilder capturedApp = null;
                    IEndpointRouteBuilder capturedEndpoints = null;

                    var hostBuilder = Host.CreateDefaultBuilder()
                        .ConfigureServices(services =>
                        {
                            services.AddCors(options =>
                            {
                                options.AddPolicy("AllowAll",
                                    builder =>
                                    {
                                        builder
                                            .AllowAnyOrigin()
                                            .AllowAnyMethod()
                                            .AllowAnyHeader();
                                    });
                            });
                        })
                        .ConfigureWebHostDefaults(webBuilder =>
                        {
                            webBuilder.Configure(app => {
                                capturedApp = app.New();
                                capturedApp.UseRouting();
                                capturedApp.UseCors("AllowAll");
                                capturedApp.UseEndpoints(endpoints =>
                                {
                                    capturedEndpoints = endpoints;
                                });

                                app.Use(next =>
                                    httpContext =>
                                       capturedApp.Build()(httpContext));
                            });
                        })
                        .ConfigureLogging(loggingBuilder =>
                        {
                            loggingBuilder.ClearProviders();
                            loggingBuilder.AddSimpleConsole(options => options.ColorBehavior = LoggerColorBehavior.Disabled);
                            loggingBuilder.AddProvider(new InteractiveLoggerProvider());
                        });

                    await hostBuilder.Build().StartAsync();

                    var httpClient = new HttpClient(new LogCapturingDelegatingHandler(new SocketsHttpHandler()))
                    {
                        BaseAddress = new Uri("http://localhost:5000")
                    };

                    await kernel.SetVariableAsync("App", capturedApp, typeof(IApplicationBuilder));
                    await kernel.SetVariableAsync("Endpoints", capturedEndpoints, typeof(IEndpointRouteBuilder));
                    await kernel.SetVariableAsync("HttpClient", httpClient);

                    await kernel.SendAsync(new SubmitCode(_prelude), CancellationToken.None);
                })
            };

            kernel.AddDirective(directive);

            Formatter.Register<HttpResponseMessage>((responseMessage, textWriter) =>
            {
                // Formatter.Register() doesn't support async formatters yet.
                // Prevent SynchronizationContext-induced deadlocks given the following sync-over-async code.
                ExecutionContext.SuppressFlow();

                try
                {
                    FormatHttpResponseMessage(responseMessage, textWriter).Wait();
                }
                finally
                {
                    ExecutionContext.RestoreFlow();
                }
            }, HtmlFormatter.MimeType);

            //KernelInvocationContext.Current.Publish(...)
            //kernel.AddMiddleware()

            return kernel;
        }

        private static async Task FormatHttpResponseMessage(HttpResponseMessage responseMessage, TextWriter textWriter)
        {
            //textWriter.WriteLine("<script>fetch('http://localhost:5000/Endpoint');</script>");

            var requestMessage = responseMessage.RequestMessage;
            var requestUri = requestMessage.RequestUri.ToString();
            var requestContent = requestMessage.Content is {} ?
                await requestMessage.Content.ReadAsStringAsync().ConfigureAwait(false) :
                string.Empty;

            var responseContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

            static dynamic HeaderTable(HttpHeaders headers) =>
                table(
                   thead(tr(
                       th("Name"), th("Value"))),
                   tbody(headers.Select(header => tr(
                       td(header.Key), td(string.Join("; ", header.Value))))));

            const string containerCssClass = "http-response-message-container";
            var flexCss = new HtmlString($@"
.{containerCssClass} {{
    display: flex;
    flex-wrap: wrap;
}}

.{containerCssClass} > div {{
    margin: .5em;
    padding: 1em;
    border: 1px solid;
}}

.{containerCssClass} > div > h2 {{
    margin-block-start: 0;
}}");

            var requestLine = h3($"{requestMessage.Method} ", a[href: requestUri](requestUri), $" HTTP/{requestMessage.Version}");
            var requestHeaders = details(summary("Headers"), HeaderTable(requestMessage.Headers));
            var requestBody = details(summary("Body"), pre(requestContent));

            var responseLine = h3($"HTTP/{responseMessage.Version} {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase}");
            var responseHeaders = details[open: true](summary("Headers"), HeaderTable(responseMessage.Headers));
            var responseBody = details[open: true](summary("Body"), pre(responseContent));

            var output = div[@class: containerCssClass](
                style[type: "text/css"](flexCss),
                div(h2("Request"), hr(), requestLine, requestHeaders, requestBody),
                div(h2("Response"), hr(), responseLine, responseHeaders, responseBody));

            Pocket.LoggerExtensions.Info(Pocket.Logger.Log, output.ToString());
            output.WriteTo(textWriter, HtmlEncoder.Default);

            if (requestMessage.Options.TryGetValue(new HttpRequestOptionsKey<ConcurrentQueue<(LogLevel, EventId, string, Exception)>>(_aspnetLogsKey), out var aspnetLogs))
            {
                details(summary("Logs"), aspnetLogs).WriteTo(textWriter, HtmlEncoder.Default);
            }
        }

        private class LogCapturingDelegatingHandler : DelegatingHandler
        {
            public LogCapturingDelegatingHandler(HttpMessageHandler innerHandler)
                : base(innerHandler)
            {
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var logs = new ConcurrentQueue<(LogLevel, EventId, string, Exception)>();
                request.Options.Set(new HttpRequestOptionsKey<ConcurrentQueue<(LogLevel, EventId, string, Exception)>>(_aspnetLogsKey), logs);

                void LogHandler(LogLevel logLevel, EventId eventId, string message, Exception exception)
                {
                    logs.Enqueue((logLevel, eventId, message, exception));
                }

                InteractiveLoggerProvider.Posted += LogHandler;

                try
                {
                    return await base.SendAsync(request, cancellationToken);
                }
                finally
                {
                    InteractiveLoggerProvider.Posted -= LogHandler;
                }
            }
        }

        private class InteractiveLoggerProvider : ILoggerProvider
        {
            public static event Action<LogLevel, EventId, string, Exception> Posted;

            public ILogger CreateLogger(string categoryName)
            {
                return new InteractiveLogger();
            }

            public void Dispose()
            {
            }

            private class InteractiveLogger : ILogger, IDisposable
            {
                public IDisposable BeginScope<TState>(TState state)
                {
                    //throw new NotImplementedException();
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
                    Posted?.Invoke(logLevel, eventId, message, exception);
                }

                public void Dispose()
                {
                }
            }
        }
    }
}
