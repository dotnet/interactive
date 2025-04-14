// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.DotNet.Interactive.App.ParserServer;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.DotNet.Interactive.VSCode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pocket;
using static Pocket.Logger;

namespace Microsoft.DotNet.Interactive.App.CommandLine;

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

    public delegate Task StartKernelHost(
        StartupOptions startupOptions,
        KernelHost kernelHost,
        IConsole console);

    public delegate Task StartNotebookParser(
        NotebookParserServer notebookParserServer,
        DirectoryInfo logPath = null);

    public delegate Task StartHttp(
        StartupOptions options,
        IConsole console,
        StartServer startServer = null,
        InvocationContext context = null);

    public static Parser Create(
        IServiceCollection services,
        StartServer startServer = null,
        Jupyter jupyter = null,
        StartKernelHost startKernelHost = null,
        StartNotebookParser startNotebookParser = null,
        StartHttp startHttp = null,
        Action onServerStarted = null,
        TelemetrySender telemetrySender = null)
    {
        var operation = Log.OnEnterAndExit();

        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        var disposeOnQuit = new CompositeDisposable();

        startServer ??= (startupOptions, invocationContext) =>
        {
            operation.Info("constructing webhost");
            var webHost = Program.ConstructWebHost(startupOptions);
            disposeOnQuit.Add(webHost);
            operation.Info("starting  kestrel server");
            webHost.Start();
            onServerStarted?.Invoke();
            webHost.WaitForShutdown();
            operation.Dispose();
        };

        jupyter ??= JupyterCommand.Do;

        startKernelHost ??= StdIoMode.Do;

        startNotebookParser ??= ParseNotebookCommand.RunParserServer;

        startHttp ??= HttpCommand.Do;

        // Setup first time use notice sentinel.
        var buildInfo = BuildInfo.GetBuildInfo(typeof(Program).Assembly);

        // Setup telemetry.
        telemetrySender ??= new TelemetrySender(
            buildInfo.AssemblyInformationalVersion,
            new FirstTimeUseNoticeSentinel(buildInfo.AssemblyInformationalVersion));

        var verboseOption = new Option<bool>(
            "--verbose",
            LocalizationResources.Cli_dotnet_interactive_verbose_Description());

        var logPathOption = new Option<DirectoryInfo>(
            "--log-path",
            LocalizationResources.Cli_dotnet_interactive_log_path_Description());

        var pathOption = new Option<DirectoryInfo>(
                "--path",
                LocalizationResources.Cli_dotnet_interactive_jupyter_install_path_Description())
            .ExistingOnly();

        var defaultKernelOption = new Option<string>(
            "--default-kernel",
            description: LocalizationResources.Cli_dotnet_interactive_jupyter_default_kernel_Description(),
            getDefaultValue: () => "csharp").AddCompletions("fsharp", "csharp", "pwsh");

        var rootCommand = DotnetInteractive();

        rootCommand.AddCommand(Jupyter());
        rootCommand.AddCommand(StdIO());
        rootCommand.AddCommand(NotebookParser());

        var eventBuilder = new StartupTelemetryEventBuilder(Sha256Hasher.ToSha256HashWithNormalizedCasing);

        return new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .CancelOnProcessTermination()
            .AddMiddleware(async (context, next) =>
            {
                if (context.ParseResult.Errors.Count == 0)
                {
                    telemetrySender.TrackStartupEvent(context.ParseResult, eventBuilder);
                }

                // If sentinel does not exist, print the welcome message showing the telemetry notification.
                if (!TelemetrySender.SkipFirstTimeExperience &&
                    !telemetrySender.FirstTimeUseNoticeSentinelExists())
                {
                    context.Console.Out.WriteLine();
                    context.Console.Out.WriteLine(TelemetrySender.WelcomeMessage);

                    telemetrySender.CreateFirstTimeUseNoticeSentinelIfNotExists();
                }

                await next(context);
            })
            .Build();

        RootCommand DotnetInteractive()
        {
            var command = new RootCommand
            {
                Name = "dotnet-interactive",
                Description = LocalizationResources.Cli_dotnet_interactive_Description()
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
                description: LocalizationResources.Cli_dotnet_interactive_jupyter_install_http_port_range_Description(),
                isDefault: true);

            var httpLocalOnlyOption = new Option<bool>(
                "--http-local-only",
                description: LocalizationResources.Cli_dotnet_interactive_jupyter_http_local_only_Description()
            );

            var jupyterCommand = new Command("jupyter", LocalizationResources.Cli_dotnet_interactive_jupyter_Description())
            {
                defaultKernelOption,
                httpLocalOnlyOption,
                httpPortRangeOption,
                new Argument<FileInfo>
                {
                    Name = "connection-file",
                    Description = LocalizationResources.Cli_dotnet_interactive_jupyter_connection_file_Description()
                }.ExistingOnly()
            };

            jupyterCommand.Handler = CommandHandler.Create<StartupOptions, JupyterOptions, IConsole, InvocationContext, CancellationToken>(JupyterHandler);

            var installCommand = new Command("install", LocalizationResources.Cli_dotnet_interactive_jupyter_install_Description())
            {
                httpPortRangeOption,
                pathOption
            };

            installCommand.Handler = CommandHandler.Create<InvocationContext, HttpPortRange, DirectoryInfo>((context, httpPortRange, path) => JupyterInstallHandler(httpPortRange, path, context));

            jupyterCommand.AddCommand(installCommand);

            return jupyterCommand;

            async Task<int> JupyterHandler(StartupOptions startupOptions, JupyterOptions options, IConsole console, InvocationContext context, CancellationToken cancellationToken)
            {
                var frontendEnvironment = new HtmlNotebookFrontendEnvironment();
                var kernel = KernelBuilder.CreateKernel(options.DefaultKernel, frontendEnvironment, startupOptions, telemetrySender);
                cancellationToken.Register(kernel.Dispose);

                await JupyterClientKernelExtension.LoadAsync(kernel);

                services.AddKernel(kernel);

                var clientSideKernelClient = new SignalRBackchannelKernelClient();

                services.AddSingleton(_ => ConnectionInformation.Load(options.ConnectionFile))
                    .AddSingleton(clientSideKernelClient)
                    .AddSingleton(c =>
                    {
                        return new JupyterRequestContextScheduler(delivery => c.GetRequiredService<JupyterRequestContextHandler>()
                            .Handle(delivery));
                    })
                    .AddSingleton(_ => new JupyterRequestContextHandler(kernel))
                    .AddSingleton<IHostedService, Shell>()
                    .AddSingleton<IHostedService, Heartbeat>();

                var result = await jupyter(startupOptions, console, startServer, context);

                return result;
            }

            Task<int> JupyterInstallHandler(HttpPortRange httpPortRange, DirectoryInfo path, InvocationContext context)
            {
                var jupyterInstallCommand = new JupyterInstallCommand(new JupyterKernelSpecInstaller(context.Console), httpPortRange, path);
                return jupyterInstallCommand.InvokeAsync();
            }
        }

        Command StdIO()
        {
            var httpPortRangeOption = new Option<HttpPortRange>(
                "--http-port-range",
                parseArgument: result => result.Tokens.Count == 0 ? HttpPortRange.Default : ParsePortRangeOption(result),
                description: LocalizationResources.Cli_dotnet_interactive_stdio_http_port_range_Description());

            var httpPortOption = new Option<HttpPort>(
                "--http-port",
                description: LocalizationResources.Cli_dotnet_interactive_stdio_http_port_Description(),
                parseArgument: result =>
                {
                    if (result.FindResultFor(httpPortRangeOption) is { } conflictingOption)
                    {
                        var parsed = result.Parent as OptionResult;
                        result.ErrorMessage =
                            LocalizationResources.Cli_dotnet_interactive_stdio_http_port_ErrorMessageCannotSpecifyBoth(conflictingOption.Token.Value, parsed.Token.Value);
                        return null;
                    }

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
                        result.ErrorMessage = LocalizationResources.Cli_dotnet_interactive_stdio_http_port_ErrorMessageMustSpecifyPortNumber();
                        return null;
                    }

                    return new HttpPort(portNumber);
                });

            var httpLocalOnlyOption = new Option<bool>(
                "--http-local-only",
                description: LocalizationResources.Cli_dotnet_interactive_jupyter_http_local_only_Description()
            );

            var kernelHostOption = new Option<Uri>(
                "--kernel-host",
                parseArgument: x => x.Tokens.Count == 0 ? KernelHost.CreateHostUriForCurrentProcessId() : KernelHost.CreateHostUri(x.Tokens[0].Value),
                isDefault: true,
                description: LocalizationResources.Cli_dotnet_interactive_stdio_kernel_host_Description());

            var previewOption = new Option<bool>("--preview", description: LocalizationResources.Cli_dotnet_interactive_stdio_preview_Description());

            var workingDirOption = new Option<DirectoryInfo>(
                "--working-dir",
                () => new DirectoryInfo(Environment.CurrentDirectory),
                LocalizationResources.Cli_dotnet_interactive_stdio_working_directory_Description());

            var stdIOCommand = new Command(
                "stdio",
                LocalizationResources.Cli_dotnet_interactive_stdio_Description())
            {
                defaultKernelOption,
                httpPortRangeOption,
                httpPortOption,
                httpLocalOnlyOption,
                kernelHostOption,
                previewOption,
                workingDirOption
            };

            stdIOCommand.Handler = CommandHandler.Create<StartupOptions, StdIOOptions, IConsole, InvocationContext, CancellationToken>(
                async (startupOptions, options, console, context, cancellationToken) =>
                {
                    using var _ =
                        console is TestConsole
                            ? Disposable.Empty
                            : Program.StartToolLogging(startupOptions.LogPath);

                    using var operation = Log.OnEnterAndExit();
                    operation.Trace("Command line: {0}", Environment.CommandLine);
                    operation.Trace("Process ID: {0}", Environment.ProcessId);

                    Console.InputEncoding = Encoding.UTF8;
                    Console.OutputEncoding = Encoding.UTF8;
                    Environment.CurrentDirectory = startupOptions.WorkingDir.FullName;

                    FrontendEnvironment frontendEnvironment = startupOptions.EnableHttpApi
                                                                  ? new HtmlNotebookFrontendEnvironment()
                                                                  : new BrowserFrontendEnvironment();

                    var kernel = KernelBuilder.CreateKernel(
                        options.DefaultKernel,
                        frontendEnvironment,
                        startupOptions,
                        telemetrySender);

                    services.AddKernel(kernel);

                    cancellationToken.Register(() => kernel.Dispose());

                    var sender = KernelCommandAndEventSender.FromTextWriter(
                        Console.Out,
                        KernelHost.CreateHostUri("stdio"));

                    var receiver = KernelCommandAndEventReceiver.FromTextReader(Console.In);

                    var host = kernel.UseHost(
                        sender,
                        receiver,
                        startupOptions.KernelHost);

                    kernel.UseQuitCommand(() =>
                    {
                        host.Dispose();
                        Environment.Exit(0);
                        return Task.CompletedTask;
                    });

                    var isVSCode = context.ParseResult.Directives.Contains("vscode") ||
                                   !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CODESPACES"));

                    if (isVSCode)
                    {
                        await VSCodeClientKernelExtension.LoadAsync(kernel);
                    }

                    if (startupOptions.EnableHttpApi)
                    {
                        var clientSideKernelClient = new SignalRBackchannelKernelClient();

                        services.AddSingleton(clientSideKernelClient);

                        if (isVSCode)
                        {
                            ((HtmlNotebookFrontendEnvironment)frontendEnvironment).RequiresAutomaticBootstrapping = false;
                        }
                        else
                        {
                            kernel.Add(
                                new JavaScriptKernel(clientSideKernelClient).UseValueSharing(),
                                new[] { "js" });
                        }

                        onServerStarted ??= () =>
                        {
                            var _ = host.ConnectAsync();
                        };
                        await startHttp(startupOptions, console, startServer, context);
                    }
                    else
                    {
                        await startKernelHost(startupOptions, host, console);
                    }

                    return 0;
                });

            return stdIOCommand;
        }

        Command NotebookParser()
        {
            var notebookParserCommand = new Command(
                "notebook-parser",
                LocalizationResources.Cli_dotnet_interactive_notebook_parserDescription());
            notebookParserCommand.Handler = CommandHandler.Create(async (InvocationContext context) =>
            {
                Console.InputEncoding = Encoding.UTF8;
                Console.OutputEncoding = Encoding.UTF8;
                var notebookParserServer = new NotebookParserServer(Console.In, Console.Out);
                context.GetCancellationToken().Register(() => notebookParserServer.Dispose());
                await startNotebookParser(notebookParserServer, context.ParseResult.GetValueForOption(logPathOption));
            });
            return notebookParserCommand;
        }

        static HttpPortRange ParsePortRangeOption(ArgumentResult result)
        {
            var source = result.Tokens[0].Value;

            if (string.IsNullOrWhiteSpace(source))
            {
                result.ErrorMessage = LocalizationResources.Cli_ErrorMessageMustSpecifyPortRange();
                return null;
            }

            var parts = source.Split(new[] { "-" }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                result.ErrorMessage = LocalizationResources.Cli_ErrorMessageMustSpecifyPortRange();
                return null;
            }

            if (!int.TryParse(parts[0], out var start) || !int.TryParse(parts[1], out var end))
            {
                result.ErrorMessage = LocalizationResources.CliErrorMessageMustSpecifyPortRangeAsStartPortEndPort();
                return null;
            }

            if (start > end)
            {
                result.ErrorMessage = LocalizationResources.CliErrorMessageStartPortMustBeLower();
                return null;
            }

            var pr = new HttpPortRange(start, end);
            return pr;
        }
    }
}