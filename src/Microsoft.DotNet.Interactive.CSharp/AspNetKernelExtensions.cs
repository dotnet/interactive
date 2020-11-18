// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.CSharp
{
    public static class AspNetKernelExtensions
    {
        public static CSharpKernel UseAspNet(
            this CSharpKernel kernel)
        {
            var command = new SubmitCode(@"
Environment.SetEnvironmentVariable($""ASPNETCORE_{WebHostDefaults.PreventHostingStartupKey}"", ""true"");

class WriteLineHandler : DelegatingHandler
{
    public WriteLineHandler(HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Console.WriteLine($""(HttpClient Request) {request}"");
        if (request.Content != null)
        {
            Console.WriteLine(await request.Content.ReadAsStringAsync());
        }

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        Console.WriteLine($""(HttpClient Response) {response}"");
        if (response.Content != null)
        {
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }

        return response;
    }
}

var HttpClient = new HttpClient(new WriteLineHandler(new SocketsHttpHandler()));
HttpClient.BaseAddress = new Uri(""http://localhost:5000/"");

private static int __AspNet_NextEndpointOrder;

public static IEndpointConventionBuilder MapAction(
    this IEndpointRouteBuilder endpoints,
    string pattern,
    RequestDelegate requestDelegate)
{
    var order = __AspNet_NextEndpointOrder--;
    var builder = endpoints.MapGet(pattern, requestDelegate);
    builder.Add(b => ((RouteEndpointBuilder)b).Order = order);
    return builder;
}

class LogLevelMonitor : IOptionsMonitor<LoggerFilterOptions>
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

    class ChangeTrackerDisposable : IDisposable
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

static LogLevelMonitor __AspNet_LogLevelMonitor = new LogLevelMonitor(new LoggerFilterOptions
{
    MinLevel = LogLevel.Warning,
});

public static class Logging
{
    public static LogLevel MinLevel
    {
        set
        {
            __AspNet_LogLevelMonitor.CurrentValue = new LoggerFilterOptions { MinLevel = value };
        }
    }
}

IApplicationBuilder App = null;
IEndpointRouteBuilder Endpoints = null;

var __AspNet_HostBuilder = Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IOptionsMonitor<LoggerFilterOptions>>(__AspNet_LogLevelMonitor);
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

            kernel.DeferCommand(command);

            return kernel;
        }
    }
}
