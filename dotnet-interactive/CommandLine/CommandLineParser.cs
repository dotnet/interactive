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
using System.Text.Encodings.Web;
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
using Microsoft.DotNet.Interactive.Server;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

            startServer ??= (startupOptions, invocationContext) =>
                Program.ConstructWebHost(startupOptions).Run();

            jupyter ??= JupyterCommand.Do;

            startKernelServer ??= async (startupOptions, kernel, console) =>
            {
                var disposable = Program.StartToolLogging(startupOptions);

                if (kernel is KernelBase kernelBase)
                {
                    kernelBase.RegisterForDisposal(disposable);
                }

                var server = new StandardIOKernelServer(
                    kernel, 
                    Console.In, 
                    Console.Out);

                await server.Input.LastAsync();
            };

            // Setup first time use notice sentinel.
            firstTimeUseNoticeSentinel ??= new FirstTimeUseNoticeSentinel(VersionSensor.Version().AssemblyInformationalVersion);

            // Setup telemetry.
            telemetry ??= new Telemetry.Telemetry(
                VersionSensor.Version().AssemblyInformationalVersion,
                firstTimeUseNoticeSentinel);
            var filter = new TelemetryFilter(Sha256Hasher.HashWithNormalizedCasing);

            var verboseOption = new Option<bool>(
                "--verbose",
                "Enable verbose logging to the console");
            
            var httpPortOption = new Option<int>(
                "--http-port",
                "Specifies the port on which to enable HTTP services");

            var logPathOption = new Option<DirectoryInfo>(
                "--log-path",
                "Enable file logging to the specified directory");

            var defaultKernelOption = new Option<string>(
                "--default-kernel", 
                description: "The default language for the kernel",
                getDefaultValue: () => "csharp");
            var rootCommand = DotnetInteractive();

            rootCommand.AddCommand(Jupyter());
            rootCommand.AddCommand(KernelServer());
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

                command.AddOption(logPathOption);
                command.AddOption(verboseOption);
                command.AddOption(httpPortOption);

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

                jupyterCommand.Handler = CommandHandler.Create<StartupOptions, JupyterOptions, IConsole, InvocationContext>(JupyterHandler);

                var installCommand = new Command("install", "Install the .NET kernel for Jupyter")
                {
                    logPathOption,
                    verboseOption
                };

                installCommand.Handler = CommandHandler.Create<IConsole, InvocationContext>(InstallHandler);

                jupyterCommand.AddCommand(installCommand);

                return jupyterCommand;

                Task<int> JupyterHandler(StartupOptions startupOptions, JupyterOptions options, IConsole console, InvocationContext context)
                {
                    services.AddSingleton(c => ConnectionInformation.Load(options.ConnectionFile))
                            .AddSingleton(_ =>
                            {
                                var frontendEnvironment = new BrowserFrontendEnvironment
                                {
                                    ApiUri = new Uri($"http://localhost:{startupOptions.HttpPort}")
                                };
                                return frontendEnvironment;
                            })
                            .AddSingleton<FrontendEnvironment>(c => c.GetService<BrowserFrontendEnvironment>())
                            .AddSingleton(c =>
                            {
                                return CommandScheduler.Create<JupyterRequestContext>(delivery => c.GetRequiredService<ICommandHandler<JupyterRequestContext>>()
                                                                                                   .Trace()
                                                                                                   .Handle(delivery));
                            })
                            .AddSingleton(c =>
                            {
                                var frontendEnvironment = c.GetRequiredService<BrowserFrontendEnvironment>();
                                
                                var kernel = CreateKernel(options.DefaultKernel,
                                    frontendEnvironment,
                                    startupOptions,
                                    c.GetRequiredService<HttpProbingSettings>());
                                return kernel;
                            })
                            .AddSingleton(c => new JupyterRequestContextHandler(
                                                  c.GetRequiredService<IKernel>())
                                              .Trace())
                            .AddSingleton<IHostedService, Shell>()
                            .AddSingleton<IHostedService, Heartbeat>()
                          ;

                    return jupyter(startupOptions, console, startServer, context);
                }

                Task<int> InstallHandler(IConsole console, InvocationContext context) =>
                    new JupyterInstallCommand(console, new JupyterKernelSpec()).InvokeAsync();
            }

            Command HttpServer()
            {
                var startKernelHttpCommand = new Command("http", "Starts dotnet-interactive with kernel functionality exposed over http")
                {
                    defaultKernelOption,
                    logPathOption,
                };

                startKernelHttpCommand.Handler = CommandHandler.Create<StartupOptions, KernelHttpOptions, IConsole, InvocationContext>(
                    (startupOptions, options, console, context) =>
                    {
                        var frontendEnvironment = new BrowserFrontendEnvironment();
                        services
                            .AddSingleton(_ => frontendEnvironment)
                            .AddSingleton(c => CreateKernel(options.DefaultKernel, frontendEnvironment, startupOptions,null));

                        return jupyter(startupOptions, console, startServer, context);
                    });

                return startKernelHttpCommand;
            }

            Command KernelServer()
            {
                var startKernelServerCommand = new Command(
                    "stdio", 
                    "Starts dotnet-interactive with kernel functionality exposed over standard I/O")
                {
                    defaultKernelOption,
                    logPathOption,
                };

                startKernelServerCommand.Handler = CommandHandler.Create<StartupOptions, KernelServerOptions, IConsole, InvocationContext>(
                    (startupOptions, options, console, context) => startKernelServer(
                        startupOptions,
                        CreateKernel(options.DefaultKernel,
                                     new BrowserFrontendEnvironment(), startupOptions,null), console));

                return startKernelServerCommand;
            }
        }

        private static IKernel CreateKernel(
            string defaultKernelName, 
            FrontendEnvironment frontendEnvironment, 
            StartupOptions startupOptions, 
            HttpProbingSettings httpProbingSettings)
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
                    .UseMathAndLaTeX());

            compositeKernel.Add(
                new FSharpKernel()
                    .UseDefaultFormatting()
                    .UseKernelHelpers()
                    .UseWho()
                    .UseDefaultNamespaces()
                    .UseXplot()
                    .UseMathAndLaTeX());

            compositeKernel.Add(
                new PowerShellKernel()
                    .UseJupyterHelpers()
                    .UseXplot(),
                new[] { "#!pwsh" });

            compositeKernel.Add(
                new JavaScriptKernel(),
                new[] { "#!js" });
            
            compositeKernel.Add(
                new HtmlKernel());

            var kernel = compositeKernel
                         .UseDefaultMagicCommands()
                         .UseLog()
                         .UseAbout()
                         .UseHttpApi(startupOptions, httpProbingSettings);
            
            SetUpFormatters(frontendEnvironment);
            

            kernel.DefaultKernelName = defaultKernelName;
            var enableHttp = new SubmitCode("#!enable-http", compositeKernel.Name);
            enableHttp.PublishInternalEvents();
            compositeKernel.DeferCommand(enableHttp);
            return kernel;
        }

        public static void SetUpFormatters(FrontendEnvironment frontendEnvironment)
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
                    Formatter<ScriptContent>.Register((script, writer) =>
                    {
                        var fullCode = $@"createDotnetInteractiveClient('{browserFrontendEnvironment.ApiUri.AbsoluteUri}').then(function (interactive) {{
let notebookScope = getDotnetInteractiveScope('{browserFrontendEnvironment.ApiUri.AbsoluteUri}');
{script.ScriptValue}
}});";
                        IHtmlContent content = PocketViewTags.script[type: "text/javascript"](fullCode.ToHtmlContent());
                        content.WriteTo(writer, HtmlEncoder.Default);
                    }, HtmlFormatter.MimeType);

                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(frontendEnvironment));
            }
        }
    }
}