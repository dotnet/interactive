// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.Parsing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.Extensions.DependencyInjection;
using Pocket;
using Serilog.Sinks.RollingFileAlternate;
using static Pocket.Logger<Microsoft.DotNet.Interactive.App.Program>;
using SerilogLoggerConfiguration = Serilog.LoggerConfiguration;

namespace Microsoft.DotNet.Interactive.App;

public class Program
{
    private static readonly ServiceCollection _serviceCollection = new();

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        SetCultureFromEnvironmentVariables();

        return await CommandLineParser.Create(_serviceCollection).InvokeAsync(args);
    }

    public static void SetCultureFromEnvironmentVariables()
    {
        var culture = Environment.GetEnvironmentVariable("DOTNET_CLI_CULTURE");
        var uiLanguage = Environment.GetEnvironmentVariable("DOTNET_CLI_UI_LANGUAGE");

        if (!string.IsNullOrWhiteSpace(culture))
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
        }

        if (!string.IsNullOrWhiteSpace(uiLanguage))
        {
            CultureInfo.CurrentUICulture = new CultureInfo(uiLanguage);
        }
    }

    private static readonly Assembly[] _assembliesEmittingPocketLoggerLogs =
    {
        typeof(Startup).Assembly, // dotnet-interactive.dll
        typeof(Kernel).Assembly, // Microsoft.DotNet.Interactive.dll
        typeof(JupyterRequestContext).Assembly, // Microsoft.DotNet.Interactive.Jupyter.dll
        typeof(PowerShellKernel).Assembly, // Microsoft.DotNet.Interactive.PowerShell.dll
        typeof(InteractiveDocument).Assembly, // Microsoft.DotNet.Interactive.Documents.dll
    };

    internal static IDisposable StartToolLogging(DirectoryInfo logPath = null)
    {
        var disposables = new CompositeDisposable();

        if (logPath is not null)
        {
            var log = new SerilogLoggerConfiguration()
                .WriteTo
                .RollingFileAlternate(logPath.FullName, outputTemplate: "{Message}{NewLine}")
                .CreateLogger();

            var subscription = LogEvents.Subscribe(
                e => log.Information(e.ToLogString()),
                _assembliesEmittingPocketLoggerLogs);

            disposables.Add(subscription);
            disposables.Add(log);
        }

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Log.Warning($"{nameof(TaskScheduler.UnobservedTaskException)}", args.Exception);
            args.SetObserved();
        };

        return disposables;
    }

    public static IWebHostBuilder ConstructWebHostBuilder(
        StartupOptions options,
        IServiceCollection serviceCollection)
    {
        // TODO: (ConstructWebHostBuilder) dispose me
        var disposables = new CompositeDisposable
        {
            StartToolLogging(options.LogPath)
        };

        using var _ = Log.OnEnterAndExit();

        HttpProbingSettings probingSettings = null;

        if (options.EnableHttpApi)
        {
            var httpPort = GetFreePort(options);
            options.HttpPort = httpPort;
            probingSettings = HttpProbingSettings.Create(httpPort.PortNumber, options.LocalOnlyNetworkInterfaces);
        }

        var webHost = new WebHostBuilder()
            .UseKestrel()
            .UseDotNetInteractiveHttpApi(options.EnableHttpApi, options.HttpPort, probingSettings, serviceCollection)
            .UseStartup<Startup>();

        if (options.EnableHttpApi && probingSettings is not null)
        {
            webHost = webHost.UseUrls(probingSettings.AddressList.Select(a => a.AbsoluteUri).ToArray());
        }

        return webHost;

        static HttpPort GetFreePort(StartupOptions startupOptions)
        {
            using var __ = Log.OnEnterAndExit(nameof(GetFreePort));
            if (startupOptions.HttpPort is not null && !startupOptions.HttpPort.IsAuto)
            {
                return startupOptions.HttpPort;
            }

            var currentPort = 0;
            var endPort = 0;

            if (startupOptions.HttpPortRange is not null)
            {
                currentPort = startupOptions.HttpPortRange.Start;
                endPort = startupOptions.HttpPortRange.End;
            }

            for (; currentPort <= endPort; currentPort++)
            {
                try
                {
                    var l = new TcpListener(IPAddress.Loopback, currentPort);
                    l.Start();
                    var port = ((IPEndPoint)l.LocalEndpoint).Port;
                    l.Stop();
                    return new HttpPort(port);
                }
                catch (SocketException)
                {

                }
            }

            throw new InvalidOperationException("Cannot find a port");
        }
    }

    public static IWebHost ConstructWebHost(StartupOptions options)
    {
        var webHost = ConstructWebHostBuilder(options, _serviceCollection)
            .Build();

        return webHost;
    }
}