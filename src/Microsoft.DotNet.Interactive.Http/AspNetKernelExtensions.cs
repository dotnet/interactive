// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Http
{
    public static class AspNetKernelExtensions
    {
        private const string _prelude = @"
class LogLevelController
{
    private readonly object _kernelLogLevelController;

    public LogLevelController(object kernelLogLevelController)
    {
        _kernelLogLevelController = kernelLogLevelController;
    }

    public LogLevel MinLevel
    {
        set
        {
            ((dynamic)_kernelLogLevelController).MinLevel = value;
        }
    }

    public bool EnableHttpClientTracing
    {
        set
        {
            ((dynamic)_kernelLogLevelController).EnableHttpClientTracing = value;
        }
    }
}

var Logging = new LogLevelController(__AspNet_LogLevelController);

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

                    var logLevelMonitor = new LogLevelMonitor(new LoggerFilterOptions
                    {
                        MinLevel = LogLevel.Warning,
                    });

                    IApplicationBuilder capturedApp = null;
                    IEndpointRouteBuilder capturedEndpoints = null;

                    var hostBuilder = Host.CreateDefaultBuilder()
                        .ConfigureServices(services =>
                        {
                            services.AddSingleton<IOptionsMonitor<LoggerFilterOptions>>(logLevelMonitor);
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
                        });

                    await hostBuilder.Build().StartAsync();

                    var logLevelController = new LogLevelController(logLevelMonitor);
                    var httpClient = new HttpClient(new WriteLineDelegatingHandler(new SocketsHttpHandler(), logLevelController))
                    {
                        BaseAddress = new Uri("http://localhost:5000")
                    };

                    await kernel.SetVariableAsync("App", capturedApp, typeof(IApplicationBuilder));
                    await kernel.SetVariableAsync("Endpoints", capturedEndpoints, typeof(IEndpointRouteBuilder));
                    await kernel.SetVariableAsync("HttpClient", httpClient);
                    await kernel.SetVariableAsync("__AspNet_LogLevelController", logLevelController, typeof(object));

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
        }

        // Must be public to access properties using `dynamic`
        public class LogLevelController
        {
            private readonly LogLevelMonitor _logLevelMonitor;

            internal LogLevelController(LogLevelMonitor logLevelMonitor)
            {
                _logLevelMonitor = logLevelMonitor;
            }

            public LogLevel MinLevel
            {
                set
                {
                    _logLevelMonitor.CurrentValue = new LoggerFilterOptions { MinLevel = value };
                }
            }

            public bool EnableHttpClientTracing { get; set; }
        }

        internal class LogLevelMonitor : IOptionsMonitor<LoggerFilterOptions>
        {
            LoggerFilterOptions _loggerFilterOptions;
            event Action<LoggerFilterOptions, string> _onChange;

            public LogLevelMonitor(LoggerFilterOptions initialOptions)
            {
                _loggerFilterOptions = initialOptions;
            }

            public LoggerFilterOptions CurrentValue 
            { 
                get => _loggerFilterOptions;
                set
                {
                    _loggerFilterOptions = value;
                    _onChange(value, string.Empty);
                }
            }

            public IDisposable OnChange(Action<LoggerFilterOptions, string> listener)
            {
                var disposable = new ChangeTrackerDisposable(this, listener);
                _onChange += disposable.OnChange;
                return disposable;
            }

            public LoggerFilterOptions Get(string name)
            {
                throw new NotImplementedException();
            }

            private class ChangeTrackerDisposable : IDisposable
            {
                private readonly Action<LoggerFilterOptions, string> _listener;
                private readonly LogLevelMonitor _monitor;

                public ChangeTrackerDisposable(LogLevelMonitor monitor, Action<LoggerFilterOptions, string> listener)
                {
                    _listener = listener;
                    _monitor = monitor;
                }

                public void OnChange(LoggerFilterOptions options, string name) => _listener.Invoke(options, name);

                public void Dispose() => _monitor._onChange -= OnChange;
            }
        }

        private class WriteLineDelegatingHandler : DelegatingHandler
        {
            private readonly LogLevelController _logLevelController;

            public WriteLineDelegatingHandler(HttpMessageHandler innerHandler, LogLevelController logLevelController)
                : base(innerHandler)
            {
                _logLevelController = logLevelController;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (_logLevelController.EnableHttpClientTracing)
                {
                    return TraceSendAsync(request, cancellationToken);
                }

                return base.SendAsync(request, cancellationToken);
            }

            private async Task<HttpResponseMessage> TraceSendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Console.WriteLine($"(HttpClient Request) {request}");
                if (request.Content != null)
                {
                    Console.WriteLine(await request.Content.ReadAsStringAsync());
                }

                HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

                Console.WriteLine($"(HttpClient Response) {response}");
                if (response.Content != null)
                {
                    Console.WriteLine(await response.Content.ReadAsStringAsync());
                }

                return response;
            }
        }
    }
}
