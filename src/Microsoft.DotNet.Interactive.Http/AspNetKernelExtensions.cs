// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.DotNet.Interactive.Http
{
    public static class AspNetKernelExtensions
    {
        public static T UseAspNet<T>(this T kernel)
            where T : DotNetKernel
        {
            var directive = new Command("#!aspnet", "Activate ASP.NET")
            {
                Handler = CommandHandler.Create<string, string, KernelInvocationContext>(async (from, name, context) =>
                {
                    Environment.SetEnvironmentVariable($"ASPNETCORE_{WebHostDefaults.PreventHostingStartupKey}", "true");

                    var logLevelMonitor = new LogLevelMonitor(new LoggerFilterOptions
                    {
                        MinLevel = LogLevel.Warning,
                    });

                    var code = new SubmitCode(@"
class LoggingController
{
    private readonly IOptionsMonitor<LoggerFilterOptions> _logLevelMonitor;

    public LoggingController(IOptionsMonitor<LoggerFilterOptions> logLevelMonitor)
    {
        _logLevelMonitor = logLevelMonitor;
    }

    public LogLevel MinLevel
    {
        set
        {
            ((dynamic)_logLevelMonitor).CurrentValue = new LoggerFilterOptions { MinLevel = value };
        }
    }

    public bool EnableHttpClientTracing { get; set; }
}

var Logging = new LoggingController((IOptionsMonitor<LoggerFilterOptions>)__AspNet_LogLevelMonitor);

class WriteLineDelegatingHandler : DelegatingHandler
{
    private readonly LoggingController _loggingController;

    public WriteLineDelegatingHandler(HttpMessageHandler innerHandler, LoggingController loggingController)
        : base(innerHandler)
    {
        _loggingController = loggingController;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_loggingController.EnableHttpClientTracing)
        {
            Console.WriteLine($""(HttpClient Request) {request}"");
            if (request.Content != null)
            {
                Console.WriteLine(await request.Content.ReadAsStringAsync());
            }
        }

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        if (_loggingController.EnableHttpClientTracing)
        {
            Console.WriteLine($""(HttpClient Response) {response}"");
            if (response.Content != null)
            {
                Console.WriteLine(await response.Content.ReadAsStringAsync());
            }
        }

        return response;
    }
}

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
}

var HttpClient = new HttpClient(new WriteLineDelegatingHandler(new SocketsHttpHandler(), Logging));
HttpClient.BaseAddress = new Uri(""http://localhost:5000/"");

IApplicationBuilder App = null;
IEndpointRouteBuilder Endpoints = null;

var __AspNet_HostBuilder = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IOptionsMonitor<LoggerFilterOptions>>((IOptionsMonitor<LoggerFilterOptions>)__AspNet_LogLevelMonitor);
    })
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.Configure(app => {
            App = app.New();
            App.UseRouting();
            App.UseEndpoints(endpoints =>
            {
                Endpoints = endpoints;
            });

            app.Use(next =>
                httpContext =>
                   App.Build()(httpContext));
        });
    })
    .ConfigureLogging(loggingBuilder =>
    {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddSimpleConsole(options => options.ColorBehavior = LoggerColorBehavior.Disabled);
    });

var __AspNet_HostRunAsyncTask = __AspNet_HostBuilder.Build().RunAsync();
");

                    await kernel.SetVariableAsync("__AspNet_LogLevelMonitor", logLevelMonitor, typeof(IOptionsMonitor<LoggerFilterOptions>));
                    kernel.DeferCommand(code);
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
                    var responseText = responseMessage.Content.ReadAsStringAsync().Result;
                    textWriter.Write(responseText);
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

        public class LogLevelMonitor : IOptionsMonitor<LoggerFilterOptions>
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
    }
}
