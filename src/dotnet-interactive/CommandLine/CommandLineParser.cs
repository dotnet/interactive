// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

using Clockwise;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Jupyter.Formatting;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pocket;
using Recipes;

using CommandHandler = System.CommandLine.Invocation.CommandHandler;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;

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

        public delegate Task StartStdIO(
            StartupOptions options,
            Kernel kernel,
            IConsole console);

        public delegate Task StartHttp(
            StartupOptions options,
            IConsole console,
            StartServer startServer = null,
            InvocationContext context = null);

        public static Parser Create(
            IServiceCollection services,
            StartServer startServer = null,
            Jupyter jupyter = null,
            StartStdIO startStdIO = null,
            StartHttp startHttp = null,
            ITelemetry telemetry = null,
            IFirstTimeUseNoticeSentinel firstTimeUseNoticeSentinel = null)
        {
          
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            var disposeOnQuit = new CompositeDisposable();
            
            startServer ??= (startupOptions, invocationContext) =>
            {
                var webHost = Program.ConstructWebHost(startupOptions);
                disposeOnQuit.Add(webHost);
                webHost.Run();
            };

            jupyter ??= JupyterCommand.Do;

            startStdIO ??= StdIOCommand.Do;

            startHttp ??= HttpCommand.Do;

            // Setup first time use notice sentinel.
            firstTimeUseNoticeSentinel ??= new FirstTimeUseNoticeSentinel(VersionSensor.Version().AssemblyInformationalVersion);

            var clearTextProperties = new[]
            {
                "frontend"
            };

            // Setup telemetry.
            telemetry ??= new Telemetry.Telemetry(
                VersionSensor.Version().AssemblyInformationalVersion,
                firstTimeUseNoticeSentinel,
                "dotnet/interactive/cli");

            var filter = new TelemetryFilter(
                Sha256Hasher.HashWithNormalizedCasing,
                clearTextProperties,
                (commandResult, directives, entryItems) =>
                {
                    // add frontend
                    var frontendTelemetryAdded = false;
                    foreach (var directive in directives)
                    {
                        switch (directive.Key)
                        {
                            case "vscode":
                            case "jupyter":
                            case "synapse":
                                frontendTelemetryAdded = true;
                                entryItems.Add(new KeyValuePair<string, string>("frontend", directive.Key));
                                break;
                        }
                    }

                    if (!frontendTelemetryAdded)
                    {
                        if (commandResult.Command.Name == "jupyter")
                        {
                            entryItems.Add(new KeyValuePair<string, string>("frontend", "jupyter"));
                        }
                    }
                });

            var verboseOption = new Option<bool>(
                "--verbose",
                "Enable verbose logging to the console");

            var logPathOption = new Option<DirectoryInfo>(
                "--log-path",
                "Enable file logging to the specified directory");

            var pathOption = new Option<DirectoryInfo>(
                "--path",
                "Installs the kernelspecs to the specified directory")
                .ExistingOnly();

            var defaultKernelOption = new Option<string>(
                "--default-kernel",
                description: "The default language for the kernel",
                getDefaultValue: () => "csharp");

            var rootCommand = DotnetInteractive();

            rootCommand.AddCommand(Jupyter());
            rootCommand.AddCommand(StdIO());
            rootCommand.AddCommand(HttpServer());

            return new CommandLineBuilder(rootCommand)
                   .UseDefaults()
                   .UseMiddleware(async (context, next) =>
                   {
                       if (context.ParseResult.Errors.Count == 0)
                       {
                           telemetry.SendFiltered(filter, context.ParseResult);
                       }

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

                command.AddGlobalOption(logPathOption);
                command.AddGlobalOption(verboseOption);

                return command;
            }

            Command Jupyter()
            {
                var httpPortRangeOption = new Option<HttpPortRange>(
                    "--http-port-range",
                    parseArgument: result => result.Tokens.Count == 0 ? HttpPortRange.Default : ParsePortRangeOption(result),
                    description: "Specifies the range of ports to use to enable HTTP services",
                    isDefault: true);

                var command = new Command("jupyter", "Starts dotnet-interactive as a Jupyter kernel")
                {
                    defaultKernelOption,
                    httpPortRangeOption,
                    new Argument<FileInfo>
                    {
                        Name = "connection-file"
                    }.ExistingOnly()
                };

                command.Handler = CommandHandler.Create<StartupOptions, JupyterOptions, IConsole, InvocationContext, CancellationToken>(JupyterHandler);

                var installCommand = new Command("install", "Install the .NET kernel for Jupyter")
                {
                    httpPortRangeOption,
                    pathOption
                };

                installCommand.Handler = CommandHandler.Create<IConsole, InvocationContext, HttpPortRange, DirectoryInfo>(InstallHandler);

                command.AddCommand(installCommand);

                return command;

                Task<int> JupyterHandler(StartupOptions startupOptions, JupyterOptions options, IConsole console, InvocationContext context, CancellationToken cancellationToken)
                {
                    services = RegisterKernelInServiceCollection(
                        services,
                        startupOptions,
                        options.DefaultKernel,
                        serviceCollection =>
                        {
                            serviceCollection.AddSingleton(_ => new HtmlNotebookFrontedEnvironment());
                            serviceCollection.AddSingleton<FrontendEnvironment>(c =>
                                c.GetService<HtmlNotebookFrontedEnvironment>());
                        });

                    services.AddSingleton(c => ConnectionInformation.Load(options.ConnectionFile))
                        .AddSingleton(c =>
                        {
                            return CommandScheduler.Create<JupyterRequestContext>(delivery => c.GetRequiredService<ICommandHandler<JupyterRequestContext>>()
                                .Trace()
                                .Handle(delivery));
                        })
                        .AddSingleton(c => new JupyterRequestContextHandler(
                                c.GetRequiredService<Kernel>())
                            .Trace())
                        .AddSingleton<IHostedService, Shell>()
                        .AddSingleton<IHostedService, Heartbeat>();

                    return jupyter(startupOptions, console, startServer, context);
                }

                Task<int> InstallHandler(IConsole console, InvocationContext context, HttpPortRange httpPortRange, DirectoryInfo path)
                {
                    var jupyterInstallCommand = new JupyterInstallCommand(console, new JupyterKernelSpecInstaller(console), httpPortRange, path);
                    return jupyterInstallCommand.InvokeAsync();
                }
            }

            Command HttpServer()
            {
                var httpPortOption = new Option<HttpPort>(
                    "--http-port",
                    description: "Specifies the port on which to enable HTTP services",
                    parseArgument: result =>
                    {
                        if (result.Tokens.Count == 0)
                        {
                            return HttpPort.Auto;
                        }

                        var source = result.Tokens[0].Value;

                        if (source == "*")
                        {
                            return HttpPort.Auto;
                        }

                        if (!int.TryParse(source, out var portNumber))
                        {
                            result.ErrorMessage = "Must specify a port number or *.";
                            return null;
                        }

                        return new HttpPort(portNumber);
                    },
                    isDefault: true);

                var command = new Command("http", "Starts dotnet-interactive with kernel functionality exposed over http")
                {
                    defaultKernelOption,
                    httpPortOption
                };

                command.Handler = CommandHandler.Create<StartupOptions, KernelHttpOptions, IConsole, InvocationContext>(
                    (startupOptions, options, console, context) =>
                    {
                        RegisterKernelInServiceCollection(
                            services,
                            startupOptions,
                            options.DefaultKernel,
                            serviceCollection =>
                            {
                                serviceCollection.AddSingleton(_ =>
                                {
                                    var frontendEnvironment = new BrowserFrontendEnvironment();
                                    return frontendEnvironment;
                                });
                                serviceCollection.AddSingleton<FrontendEnvironment>(c => c.GetRequiredService<BrowserFrontendEnvironment>());
                            });
                        return startHttp(startupOptions, console, startServer, context);
                    });

                return command;
            }

            Command StdIO()
            {
                var httpPortRangeOption = new Option<HttpPortRange>(
                    "--http-port-range",
                    parseArgument: result => result.Tokens.Count == 0 ? HttpPortRange.Default : ParsePortRangeOption(result),
                    description: "Specifies the range of ports to use to enable HTTP services");

                var command = new Command(
                    "stdio",
                    "Starts dotnet-interactive with kernel functionality exposed over standard I/O")
                {
                    defaultKernelOption,
                    httpPortRangeOption
                };

                command.Handler = CommandHandler.Create<StartupOptions, StdIOOptions, IConsole, InvocationContext, CancellationToken>(
                    (startupOptions, options, console, context, cancellationToken) =>
                    {
                        if (startupOptions.EnableHttpApi)
                        {
                            RegisterKernelInServiceCollection(
                                services,
                                startupOptions,
                                options.DefaultKernel,
                                serviceCollection =>
                                {
                                    serviceCollection.AddSingleton(_ => new HtmlNotebookFrontedEnvironment());
                                    serviceCollection.AddSingleton<FrontendEnvironment>(c =>
                                        c.GetService<HtmlNotebookFrontedEnvironment>());
                                }, kernel =>
                                {
                                    StdIOCommand.CreateServer(kernel, console);

                                    kernel.UseQuiCommand(disposeOnQuit, cancellationToken);
                                });

                            return startHttp(startupOptions, console, startServer, context);
                        }

                        {
                            var kernel = CreateKernel(options.DefaultKernel, new BrowserFrontendEnvironment(),
                                startupOptions);
                            disposeOnQuit.Add(kernel);
                            kernel.UseQuiCommand(disposeOnQuit, cancellationToken);
                           

                            return startStdIO(
                                startupOptions,
                                kernel,
                                console);
                        }
                    });

                return command;
            }

            static HttpPortRange ParsePortRangeOption(ArgumentResult result)
            {
                var source = result.Tokens[0].Value;

                if (string.IsNullOrWhiteSpace(source))
                {
                    result.ErrorMessage = "Must specify a port range";
                    return null;
                }

                var parts = source.Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                {
                    result.ErrorMessage = "Must specify a port range";
                    return null;
                }

                if (!int.TryParse(parts[0], out var start) || !int.TryParse(parts[1], out var end))
                {
                    result.ErrorMessage = "Must specify a port range as StartPort-EndPort";
                    return null;
                }

                if (start > end)
                {
                    result.ErrorMessage = "Start port must be lower then end port";
                    return null;
                }

                var pr = new HttpPortRange(start, end);
                return pr;
            }
        }

        private static IServiceCollection RegisterKernelInServiceCollection(IServiceCollection services, StartupOptions startupOptions, string defaultKernel, Action<IServiceCollection> configureFrontedEnvironment, Action<Kernel> afterKernelCreation = null)
        {
            configureFrontedEnvironment(services);
            services
                .AddSingleton(c =>
                {
                    var frontendEnvironment = c.GetRequiredService<FrontendEnvironment>();
                    var kernel = CreateKernel(defaultKernel, frontendEnvironment, startupOptions,
                        c.GetService<HttpProbingSettings>());

                    afterKernelCreation?.Invoke(kernel);
                    return kernel;
                })
                .AddSingleton<Kernel>(c => c.GetRequiredService<CompositeKernel>());

            return services;
        }

        private static CompositeKernel CreateKernel(
            string defaultKernelName,
            FrontendEnvironment frontendEnvironment,
            StartupOptions startupOptions,
            HttpProbingSettings httpProbingSettings = null)
        {
            var compositeKernel = new CompositeKernel();
            compositeKernel.FrontendEnvironment = frontendEnvironment;

            compositeKernel.Add(
                new CSharpKernel()
                    .UseDefaultFormatting()
                    .UseNugetDirective()
                    .UseKernelHelpers()
                    .UseJupyterHelpers()
                    .UseWho()
                    .UseXplot()
                    .UseMathAndLaTeX()
                    .UseDotNetVariableSharing(),
                new[] { "c#", "C#" });

            compositeKernel.Add(
                new FSharpKernel()
                    .UseDefaultFormatting()
                    .UseNugetDirective()
                    .UseKernelHelpers()
                    .UseWho()
                    .UseDefaultNamespaces()
                    .UseXplot()
                    .UseMathAndLaTeX()
                    .UseDotNetVariableSharing(),
                new[] { "f#", "F#" });

            compositeKernel.Add(
                new PowerShellKernel()
                    .UseJupyterHelpers()
                    .UseXplot()
                    .UseProfiles()
                    .UseDotNetVariableSharing(),
                new[] { "powershell" });

            compositeKernel.Add(
                new JavaScriptKernel(),
                new[] { "js" });

            compositeKernel.Add(
                new HtmlKernel());

            var kernel = compositeKernel
                         .UseDefaultMagicCommands()
                         .UseLog()
                         .UseAbout()
                         .UseProxyKernel();

            SetUpFormatters(frontendEnvironment, startupOptions);

            kernel.DefaultKernelName = defaultKernelName;

            if (startupOptions.EnableHttpApi)
            {
                kernel = kernel.UseHttpApi(startupOptions, httpProbingSettings);
                var enableHttp = new SubmitCode("#!enable-http", compositeKernel.Name);
                enableHttp.PublishInternalEvents();
                kernel.DeferCommand(enableHttp);
            }

            return kernel;
        }

        public static void SetUpFormatters(FrontendEnvironment frontendEnvironment, StartupOptions startupOptions)
        {
            switch (frontendEnvironment)
            {
                case AutomationEnvironment automationEnvironment:
                    break;

                case BrowserFrontendEnvironment browserFrontendEnvironment:
                    Formatter.DefaultMimeType = HtmlFormatter.MimeType;
                    Formatter.SetPreferredMimeTypeFor(typeof(LaTeXString), "text/latex");
                    Formatter.SetPreferredMimeTypeFor(typeof(MathString), "text/latex");
                    Formatter.SetPreferredMimeTypeFor(typeof(string), PlainTextFormatter.MimeType);
                    Formatter.SetPreferredMimeTypeFor(typeof(ScriptContent), HtmlFormatter.MimeType);

                    Formatter<LaTeXString>.Register((laTeX, writer) => writer.Write(laTeX.ToString()), "text/latex");
                    Formatter<MathString>.Register((math, writer) => writer.Write(math.ToString()), "text/latex");
                    if (startupOptions.EnableHttpApi && browserFrontendEnvironment is HtmlNotebookFrontedEnvironment jupyterFrontedEnvironment)
                    {
                        Formatter<ScriptContent>.Register((script, writer) =>
                        {
                            var fullCode = $@"if (typeof window.createDotnetInteractiveClient === typeof Function) {{
createDotnetInteractiveClient('{jupyterFrontedEnvironment.DiscoveredUri.AbsoluteUri}').then(function (interactive) {{
let notebookScope = getDotnetInteractiveScope('{jupyterFrontedEnvironment.DiscoveredUri.AbsoluteUri}');
{script.ScriptValue}
}});
}}";
                            IHtmlContent content =
                                PocketViewTags.script[type: "text/javascript"](fullCode.ToHtmlContent());
                            content.WriteTo(writer, HtmlEncoder.Default);
                        }, HtmlFormatter.MimeType);
                    }
                    else
                    {
                        Formatter<ScriptContent>.Register((script, writer) =>
                        {
                            IHtmlContent content =
                                PocketViewTags.script[type: "text/javascript"](script.ScriptValue.ToHtmlContent());
                            content.WriteTo(writer, HtmlEncoder.Default);
                        }, HtmlFormatter.MimeType);
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(frontendEnvironment));
            }
        }
    }
}