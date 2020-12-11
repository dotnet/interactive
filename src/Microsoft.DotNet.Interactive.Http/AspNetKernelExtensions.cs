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
                            webBuilder
                                .ConfigureKestrel(kestrelOptions =>
                                {
                                    kestrelOptions.ListenLocalhost(5000, listenOptions =>
                                    {
                                        listenOptions.UseConnectionLogging();
                                    });
                                })
                                .Configure(app => {
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
                            loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                            loggingBuilder.ClearProviders();
                            //loggingBuilder.AddSimpleConsole(options => options.ColorBehavior = LoggerColorBehavior.Disabled);
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
            var requestBodyString = requestMessage.Content is {} ?
                await requestMessage.Content.ReadAsStringAsync().ConfigureAwait(false) :
                string.Empty;

            var responseBodyString = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

            static dynamic HeaderTable(HttpHeaders headers, HttpContentHeaders contentHeaders) =>
                table(
                    thead(tr(
                        th("Name"), th("Value"))),
                    tbody((contentHeaders is null ? headers : headers.Concat(contentHeaders)).Select(header => tr(
                            td(header.Key), td(string.Join("; ", header.Value))))));

            const string containerClass = "http-response-message-container";
            const string logContainerClass = "aspnet-logs-container";
            var flexCss = new HtmlString($@"
.{containerClass} {{
    display: flex;
    flex-wrap: wrap;
}}

.{containerClass} > div {{
    margin: .5em;
    padding: 1em;
    border: 1px solid;
}}

.{containerClass} > div > h2 {{
    margin-top: 0;
}}

.{containerClass} > div > h3 {{
    margin-bottom: 0;
}}

.{containerClass} summary {{
    margin-top: 1em;
}}

.{containerClass} summary, .{logContainerClass} summary {{
    font-size: 1.17em;
    font-weight: 700;
}}

.{logContainerClass} {{
    margin: 0 .5em;
}}");

            var requestLine = h3($"{requestMessage.Method} ", a[href: requestUri](requestUri), $" HTTP/{requestMessage.Version}");
            var requestHeaders = details(summary("Headers"), HeaderTable(requestMessage.Headers, requestMessage.Content?.Headers));
            var requestBody = details(summary("Body"), pre(requestBodyString));

            var responseLine = h3($"HTTP/{responseMessage.Version} {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase}");

            var responseHeaders = details[open: true](summary("Headers"), HeaderTable(responseMessage.Headers, responseMessage.Content.Headers));
            var responseBody = details[open: true](summary("Body"), pre(responseBodyString));

            var output = div[@class: containerClass](
                style[type: "text/css"](flexCss),
                div(h2("Request"), hr(), requestLine, requestHeaders, requestBody),
                div(h2("Response"), hr(), responseLine, responseHeaders, responseBody));

            Pocket.LoggerExtensions.Info(Pocket.Logger.Log, output.ToString());
            output.WriteTo(textWriter, HtmlEncoder.Default);

            if (requestMessage.Options.TryGetValue(new HttpRequestOptionsKey<ConcurrentQueue<LogMessage>>(_aspnetLogsKey), out var aspnetLogs))
            {
                details[@class: logContainerClass](summary("Logs"), aspnetLogs).WriteTo(textWriter, HtmlEncoder.Default);
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
                var logs = new ConcurrentQueue<LogMessage>();
                request.Options.Set(new HttpRequestOptionsKey<ConcurrentQueue<LogMessage>>(_aspnetLogsKey), logs);

                InteractiveLoggerProvider.Posted += logs.Enqueue;

                try
                {
                    return await base.SendAsync(request, cancellationToken);
                }
                finally
                {
                    // Delay unregistering to give a chance for the last logs related to the request to arrive.
                    // The normal "_ =" doesn't work because of PocketView
                    var _ = Task.Run(async () =>
                    {
                        await Task.Delay(100);
                        InteractiveLoggerProvider.Posted -= logs.Enqueue;
                    });
                }
            }
        }

        private class InteractiveLoggerProvider : ILoggerProvider
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

        private class LogMessage
        {
            public LogLevel LogLevel { get; set; }
            public string Category { get; set; }
            public EventId EventId { get; set; }
            public string Message { get; set; }
            public Exception Exception { get; set; }
        }
    }
}
