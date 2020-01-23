// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.Parsing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.Extensions.DependencyInjection;
using Pocket;
using Serilog.Sinks.RollingFileAlternate;
using SerilogLoggerConfiguration = Serilog.LoggerConfiguration;
using static Pocket.Logger<Microsoft.DotNet.Interactive.App.Program>;

namespace Microsoft.DotNet.Interactive.App
{
    public class Program
    {
        private static readonly ServiceCollection _serviceCollection = new ServiceCollection();

        public static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            return await CommandLineParser.Create(_serviceCollection).InvokeAsync(args);
        }

        private static readonly Assembly[] _assembliesEmittingPocketLoggerLogs =
        {
            typeof(Startup).Assembly,
            typeof(Shell).Assembly
        };

        internal static IDisposable StartToolLogging(StartupOptions options)
        {
            var disposables = new CompositeDisposable();

            if (options.LogPath != null)
            {
                var log = new SerilogLoggerConfiguration()
                          .WriteTo
                          .RollingFileAlternate(options.LogPath.FullName, outputTemplate: "{Message}{NewLine}")
                          .CreateLogger();

                var subscription = LogEvents.Subscribe(
                    e => log.Information(e.ToLogString()),
                    _assembliesEmittingPocketLoggerLogs);

                disposables.Add(subscription);
                disposables.Add(log);
            }

            if (options.Verbose)
            {
                disposables.Add(
                    LogEvents.Subscribe(e => Console.WriteLine(e.ToLogString()),
                                        _assembliesEmittingPocketLoggerLogs));
            }

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                Log.Warning($"{nameof(TaskScheduler.UnobservedTaskException)}", args.Exception);
                args.SetObserved();
            };

            return disposables;
        }

        public static IWebHost ConstructWebHost(StartupOptions options)
        {
            var disposables = new CompositeDisposable
            {
                StartToolLogging(options)
            };

            var webHost = new WebHostBuilder()
                          .UseKestrel()
                          .UseContentRoot(Path.GetDirectoryName(typeof(Program).Assembly.Location))
                          .ConfigureServices(c =>
                          {
                              c.AddSingleton(options);

                              foreach (var serviceDescriptor in _serviceCollection)
                              {
                                  c.Add(serviceDescriptor);
                              }
                          })
                          .UseContentRoot(Path.GetDirectoryName(typeof(Program).Assembly.Location))
                          .UseStartup<Startup>()
                          .Build();

            return webHost;
        }
    }
}