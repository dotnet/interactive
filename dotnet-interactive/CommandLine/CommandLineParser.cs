// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.AspNetCore.Hosting;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Server;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FSharp.Core;
using Recipes;
using CommandHandler = System.CommandLine.Invocation.CommandHandler;

namespace Microsoft.DotNet.Interactive.App.CommandLine
{
    public static class CommandLineParser
    {
        public delegate void StartServer(
            StartupOptions options,
            InvocationContext context);

        public delegate Task<int> Jupyter(
            StartupOptions options, 
            IConsole console,
            StartServer startServer = null,
            InvocationContext context = null);

        public delegate Task StartKernelServer(
            StartupOptions options, 
            IKernel kernel,
            IConsole console);

        public static Parser Create(
            IServiceCollection services,
            StartServer startServer = null,
            Jupyter jupyter = null,
            StartKernelServer startKernelServer = null,
            ITelemetry telemetry = null,
            IFirstTimeUseNoticeSentinel firstTimeUseNoticeSentinel = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            startServer ??= ((startupOptions, invocationContext) =>
                Program.ConstructWebHost(startupOptions).Run());

            jupyter ??= JupyterCommand.Do;

            startKernelServer ??= (async (startupOptions, kernel, console) =>
            {
                var server = new StandardIOKernelServer(
                    kernel, 
                    Console.In, 
                    Console.Out);

                await server.Input.LastAsync();
            });

            // Setup first time use notice sentinel.
            firstTimeUseNoticeSentinel ??= new FirstTimeUseNoticeSentinel(VersionSensor.Version().AssemblyInformationalVersion);

            // Setup telemetry.
            telemetry ??= new Telemetry.Telemetry(
                VersionSensor.Version().AssemblyInformationalVersion,
                firstTimeUseNoticeSentinel);
            var filter = new TelemetryFilter(Sha256Hasher.HashWithNormalizedCasing);
            void Track(ParseResult o) => telemetry.SendFiltered(filter, o);

            var verboseOption = new Option<bool>(
                "--verbose",
                "Enable verbose logging to the console");

            var logPathOption = new Option<DirectoryInfo>(
                "--log-path",
                "Enable file logging to the specified directory");

            var defaultKernelOption = new Option<string>(
                "--default-kernel", 
                description: "The default language for the kernel",
                defaultValue: "csharp");
            var rootCommand = DotnetInteractive();

            rootCommand.AddCommand(Jupyter());
            rootCommand.AddCommand(KernelServer());

            return new CommandLineBuilder(rootCommand)
                   .UseDefaults()
                   .UseMiddleware(async (context, next) =>
                   {
                       // If sentinel does not exist, print the welcome message showing the telemetry notification.
                       if (!firstTimeUseNoticeSentinel.Exists() && !Telemetry.Telemetry.SkipFirstTimeExperience)
                       {
                           context.Console.Out.WriteLine();
                           context.Console.Out.WriteLine(Telemetry.Telemetry.WelcomeMessage);

                           firstTimeUseNoticeSentinel.CreateIfNotExists();
                       }

                       await next(context);
                   })
                   .Build();

            RootCommand DotnetInteractive()
            {
                var command = new RootCommand
                {
                    Name = "dotnet-interactive",
                    Description = "Interactive programming for .NET."
                };

                command.AddOption(logPathOption);
                command.AddOption(verboseOption);

                return command;
            }

            Command Jupyter()
            {
                var jupyterCommand = new Command("jupyter", "Starts dotnet-interactive as a Jupyter kernel")
                {
                    defaultKernelOption,
                    logPathOption,
                    verboseOption,
                    new Argument<FileInfo>
                    {
                        Name = "connection-file"
                    }.ExistingOnly()
                };

                jupyterCommand.Handler = CommandHandler.Create<StartupOptions, JupyterOptions, IConsole, InvocationContext>((startupOptions, options, console, context) =>
                {
                    Track(context.ParseResult);

                    services
                        .AddSingleton(c => ConnectionInformation.Load(options.ConnectionFile))
                        .AddSingleton(
                            c =>
                            {
                                return CommandScheduler
                                    .Create<JupyterRequestContext>(delivery => c.GetRequiredService<ICommandHandler<JupyterRequestContext>>()
                                                                                .Trace()
                                                                                .Handle(delivery));
                            })
                        .AddSingleton(c => CreateKernel(options.DefaultKernel))
                        .AddSingleton(c => new JupyterRequestContextHandler(c.GetRequiredService<IKernel>())
                                          .Trace())
                        .AddSingleton<IHostedService, Shell>()
                        .AddSingleton<IHostedService, Heartbeat>();

                    return jupyter(startupOptions, console, startServer, context);
                });

                var installCommand = new Command("install", "Install the .NET kernel for Jupyter")
                {
                    logPathOption,
                    verboseOption
                };

                installCommand.Handler = CommandHandler.Create<IConsole, InvocationContext>((console, context) =>
                {
                    Track(context.ParseResult);
                    return new JupyterInstallCommand(console, new JupyterKernelSpec()).InvokeAsync();
                });

                jupyterCommand.AddCommand(installCommand);

                return jupyterCommand;
            }

            Command KernelServer()
            {
                var startKernelServerCommand = new Command("kernel-server", "Starts dotnet-interactive with kernel functionality exposed over standard I/O")
                {
                    defaultKernelOption,
                    logPathOption,
                    verboseOption
                };

                startKernelServerCommand.Handler = CommandHandler.Create<StartupOptions, KernelServerOptions, IConsole, InvocationContext>(
                    (startupOptions, options, console, context) =>
                {
                    Track(context.ParseResult);
                    return startKernelServer(startupOptions, CreateKernel(options.DefaultKernel), console);
                });

                return startKernelServerCommand;
            }
        }

        private static IKernel CreateKernel(string defaultKernelName)
        {
            var kernel = new CompositeKernel
                         {
                             new CSharpKernel()
                                 .UseDefaultFormatting()
                                 .UseNugetDirective()
                                 .UseKernelHelpers()
                                 .UseWho()
                                 .UseXplot()
                                 .UseMathAndLaTeX(),
                             new FSharpKernel()
                                 .UseDefaultFormatting()
                                 .UseKernelHelpers()
                                 .UseWho()
                                 .UseDefaultNamespaces()
                                 .UseXplot()
                                 .UseMathAndLaTeX()
                         }
                         .UseDefaultMagicCommands()
                         .UseAbout();

            kernel.DefaultKernelName = defaultKernelName;
            kernel.Name = ".NET";

            return kernel;
        }
    }
}