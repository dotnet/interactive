// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
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
    public delegate void StartWebServer(
        StartupOptions options);

    public delegate Task<int> StartJupyter(
        StartupOptions options,
        StartWebServer startWebServer = null);

    public delegate Task StartKernelHost(
        StartupOptions startupOptions,
        KernelHost kernelHost);

    public delegate Task StartNotebookParser(
        NotebookParserServer notebookParserServer,
        DirectoryInfo logPath = null);

    public delegate Task StartHttp(
        StartupOptions options,
        StartWebServer startWebServer = null);

    public static RootCommand Create(
        IServiceCollection services,
        StartWebServer startWebServer = null,
        StartJupyter startJupyter = null,
        StartKernelHost startStdio = null,
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

        startWebServer ??= startupOptions =>
        {
            operation.Info("constructing webhost");
            var webHost = Program.ConstructWebHost(startupOptions);
            disposeOnQuit.Add(webHost);
            operation.Info("starting kestrel server");
            webHost.Start();
            onServerStarted?.Invoke();
            webHost.WaitForShutdown();
            operation.Dispose();
        };

        startJupyter ??= JupyterCommand.Do;

        startStdio ??= StdIoMode.Do;

        startNotebookParser ??= ParseNotebookCommand.RunParserServer;

        startHttp ??= HttpCommand.Do;

        // Setup first time use notice sentinel.
        var buildInfo = BuildInfo.GetBuildInfo(typeof(Program).Assembly);

        // Setup telemetry.
        telemetrySender ??= new TelemetrySender(
            buildInfo.AssemblyInformationalVersion,
            new FirstTimeUseNoticeSentinel(buildInfo.AssemblyInformationalVersion));

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = LocalizationResources.Cli_dotnet_interactive_verbose_Description(),
            Recursive = true
        };

        var logPathOption = new Option<DirectoryInfo>("--log-path")
        {
            Description = LocalizationResources.Cli_dotnet_interactive_log_path_Description(),
            Recursive = true
        };

        var httpLocalOnlyOption = new Option<bool>("--http-local-only")
        {
            Description = LocalizationResources.Cli_dotnet_interactive_jupyter_http_local_only_Description()
        };

        Uri ParseKernelHost(ArgumentResult x) =>
            x.Tokens.Count is 0
                ? KernelHost.CreateHostUriForCurrentProcessId()
                : KernelHost.CreateHostUri(x.Tokens[0].Value);

        var kernelHostOption = new Option<Uri>("--kernel-host")
        {
            CustomParser = ParseKernelHost,
            DefaultValueFactory = ParseKernelHost,
            Description = LocalizationResources.Cli_dotnet_interactive_stdio_kernel_host_Description()
        };

        var jupyterConnectionFileArg = new Argument<FileInfo>("connection-file")
        {
            Description = LocalizationResources.Cli_dotnet_interactive_jupyter_connection_file_Description()
        }.AcceptExistingOnly();

        var jupyterInstallPathOption = new Option<DirectoryInfo>("--path")
            {
                Description = LocalizationResources.Cli_dotnet_interactive_jupyter_install_path_Description()
            }
            .AcceptExistingOnly();

        var defaultKernelOption = new Option<string>("--default-kernel")
        {
            Description = LocalizationResources.Cli_dotnet_interactive_jupyter_default_kernel_Description(),
            DefaultValueFactory = _ => "csharp"
        };
        defaultKernelOption.CompletionSources.Add("fsharp", "csharp", "pwsh", "http");

        var workingDirOption = new Option<DirectoryInfo>("--working-dir")
        {
            DefaultValueFactory = _ => new DirectoryInfo(Environment.CurrentDirectory),
            Description = LocalizationResources.Cli_dotnet_interactive_stdio_working_directory_Description()
        };

        var rootCommand = DotnetInteractive();

        rootCommand.Add(Jupyter());
        rootCommand.Add(StdIO());
        rootCommand.Add(NotebookParser());

        // directives used for marking telemetry for different frontend clients
        rootCommand.Directives.Add(new("jupyter"));
        rootCommand.Directives.Add(new("synapse"));
        rootCommand.Directives.Add(new("vs"));
        rootCommand.Directives.Add(new("vscode"));

        return rootCommand;

        RootCommand DotnetInteractive()
        {
            var command = new RootCommand("dotnet-interactive")
            {
                Description = LocalizationResources.Cli_dotnet_interactive_Description()
            };

            command.Add(logPathOption);
            command.Add(verboseOption);

            return command;
        }

        Command Jupyter()
        {
            var httpPortRangeOption = new Option<HttpPortRange>("--http-port-range")
            {
                CustomParser = ParsePortRangeOption,
                DefaultValueFactory = ParsePortRangeOption,
                Description = LocalizationResources.Cli_dotnet_interactive_jupyter_install_http_port_range_Description()
            };

            
            var jupyterCommand = new Command("jupyter", LocalizationResources.Cli_dotnet_interactive_jupyter_Description())
            {
                defaultKernelOption,
                httpLocalOnlyOption,
                httpPortRangeOption,
                jupyterConnectionFileArg
            };

            jupyterCommand.SetAction(JupyterHandler);

            var installCommand = new Command("install")
            {
                Description = LocalizationResources.Cli_dotnet_interactive_jupyter_install_Description(),
            };
            installCommand.Add(httpPortRangeOption);
            installCommand.Add(jupyterInstallPathOption);

            installCommand.SetAction(JupyterInstallHandler);

            jupyterCommand.Add(installCommand);

            return jupyterCommand;

            async Task<int> JupyterHandler(ParseResult parseResult, CancellationToken cancellationToken) 
            {
                var startupOptions = StartupOptions.Parse(parseResult);
                var jupyterOptions = new JupyterOptions(parseResult.GetValue(jupyterConnectionFileArg), parseResult.GetValue(defaultKernelOption));
                var kernel = KernelBuilder.CreateKernel(jupyterOptions.DefaultKernel, new HtmlNotebookFrontendEnvironment(), startupOptions, telemetrySender);
                cancellationToken.Register(kernel.Dispose);

                await JupyterClientKernelExtension.LoadAsync(kernel);

                services.AddKernel(kernel);

                var clientSideKernelClient = new SignalRBackchannelKernelClient();

                services.AddSingleton(_ => ConnectionInformation.Load(jupyterOptions.ConnectionFile))
                    .AddSingleton(clientSideKernelClient)
                    .AddSingleton(c =>
                    {
                        return new JupyterRequestContextScheduler(delivery => c.GetRequiredService<JupyterRequestContextHandler>()
                            .Handle(delivery));
                    })
                    .AddSingleton(_ => new JupyterRequestContextHandler(kernel))
                    .AddSingleton<IHostedService, Shell>()
                    .AddSingleton<IHostedService, Heartbeat>();

                SendStartupTelemetry(parseResult, telemetrySender);

                var result = await startJupyter(startupOptions, startWebServer);

                return result;
            }

            Task<int> JupyterInstallHandler(ParseResult parseResult, CancellationToken cancellationToken)
            {
                SendStartupTelemetry(parseResult, telemetrySender);

                var jupyterInstallCommand = new JupyterInstallCommand(
                    new JupyterKernelSpecInstaller(
                        parseResult.InvocationConfiguration.Output,
                        parseResult.InvocationConfiguration.Error),
                    parseResult.GetValue(httpPortRangeOption),
                    parseResult.GetValue(jupyterInstallPathOption));
                return jupyterInstallCommand.InvokeAsync();
            }
        }

        Command StdIO()
        {
            // FIX: (Create) can this be removed?
            var previewOption = new Option<bool>("--preview")
            {
                Description = LocalizationResources.Cli_dotnet_interactive_stdio_preview_Description()
            };

            var httpPortRangeOption = new Option<HttpPortRange>("--http-port-range")
            {
                CustomParser = ParsePortRangeOption,
                Description = LocalizationResources.Cli_dotnet_interactive_jupyter_install_http_port_range_Description()
            };

            var httpPortOption = new Option<HttpPort>("--http-port")
            {
                Description = LocalizationResources.Cli_dotnet_interactive_stdio_http_port_Description(),
                CustomParser = result =>
                {
                    if (result.GetResult(httpPortRangeOption) is { Implicit: false } conflictingOption)
                    {
                        var parsed = (OptionResult)result.Parent;
                        result.AddError(
                            LocalizationResources.Cli_dotnet_interactive_stdio_http_port_ErrorMessageCannotSpecifyBoth(
                                conflictingOption.IdentifierToken?.Value,
                                parsed!.IdentifierToken?.Value));
                        return null;
                    }

                    if (result.Tokens.Count is 0)
                    {
                        return HttpPort.Auto;
                    }

                    var source = result.Tokens[0].Value;

                    if (source is "*")
                    {
                        return HttpPort.Auto;
                    }

                    if (!int.TryParse(source, out var portNumber))
                    {
                        result.AddError(LocalizationResources.Cli_dotnet_interactive_stdio_http_port_ErrorMessageMustSpecifyPortNumber());
                        return null;
                    }

                    return new HttpPort(portNumber);
                },
            };

            var stdIOCommand = new Command(
                "stdio",
                LocalizationResources.Cli_dotnet_interactive_stdio_Description())
            {
                defaultKernelOption,
                httpPortRangeOption,
                httpPortOption,
                kernelHostOption,
                previewOption,
                workingDirOption
            };
         
            stdIOCommand.SetAction(async (parseResult, cancellationToken) =>
            {
                using var _ =
                    parseResult.InvocationConfiguration.Output is StringWriter
                        ? Disposable.Empty
                        : Program.StartToolLogging(parseResult.GetValue(logPathOption));

                using var operation = Log.OnEnterAndExit();
                operation.Trace("Command line: {0}", Environment.CommandLine);
                operation.Trace("Process ID: {0}", Environment.ProcessId);

                var startupOptions = StartupOptions.Parse(parseResult);

                Console.InputEncoding = Encoding.UTF8;
                Console.OutputEncoding = Encoding.UTF8;
                Environment.CurrentDirectory = startupOptions.WorkingDir.FullName;

                FrontendEnvironment frontendEnvironment = startupOptions.EnableHttpApi
                                                              ? new HtmlNotebookFrontendEnvironment()
                                                              : new BrowserFrontendEnvironment();

                var kernel = KernelBuilder.CreateKernel(
                    parseResult.GetValue(defaultKernelOption),
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
                    startupOptions.KernelHostUri);

                kernel.UseQuitCommand(() =>
                {
                    host.Dispose();
                    Environment.Exit(0);
                    return Task.CompletedTask;
                });

                var isVSCode = parseResult.Tokens.Any(t => t is { Value: "[vscode]", Type: TokenType.Directive }) ||
                               !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CODESPACES"));

                if (isVSCode)
                {
                    await VSCodeClientKernelExtension.LoadAsync(kernel);
                }

                SendStartupTelemetry(parseResult, telemetrySender);

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
                            ["js"]);
                    }

                    onServerStarted ??= () =>
                    {
                        var _ = host.ConnectAsync();
                    };

                    await startHttp(startupOptions, startWebServer);
                }
                else
                {
                    await startStdio(startupOptions, host);
                }

                return 0;
            });

            return stdIOCommand;
        }

        Command NotebookParser()
        {
            var notebookParserCommand = new Command("notebook-parser")
            {
                Description = LocalizationResources.Cli_dotnet_interactive_notebook_parserDescription()
            };

            notebookParserCommand.SetAction(async (parseResult, cancellationToken) =>
            {
                Console.InputEncoding = Encoding.UTF8;
                Console.OutputEncoding = Encoding.UTF8;
                var notebookParserServer = new NotebookParserServer(Console.In, Console.Out);
                cancellationToken.Register(() => notebookParserServer.Dispose());
                await startNotebookParser(notebookParserServer, parseResult.GetValue(logPathOption));
            });
            return notebookParserCommand;
        }

        static HttpPortRange ParsePortRangeOption(ArgumentResult result)
        {
            if (result.Tokens.Count == 0)
            {
                return HttpPortRange.Default;
            }

            var source = result.Tokens[0].Value;

            if (string.IsNullOrWhiteSpace(source))
            {
                result.AddError(LocalizationResources.Cli_ErrorMessageMustSpecifyPortRange());
                return null;
            }

            var parts = source.Split(["-"], StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                result.AddError(LocalizationResources.Cli_ErrorMessageMustSpecifyPortRange());
                return null;
            }

            if (!int.TryParse(parts[0], out var start) || !int.TryParse(parts[1], out var end))
            {
                result.AddError(LocalizationResources.CliErrorMessageMustSpecifyPortRangeAsStartPortEndPort());
                return null;
            }

            if (start > end)
            {
                result.AddError(LocalizationResources.CliErrorMessageStartPortMustBeLower());
                return null;
            }

            var pr = new HttpPortRange(start, end);
            return pr;
        }
    }

    private static void SendStartupTelemetry(ParseResult parseResult, TelemetrySender telemetrySender)
    {
        var eventBuilder = new StartupTelemetryEventBuilder(Sha256Hasher.ToSha256HashWithNormalizedCasing);

        if (parseResult.Errors.Count == 0)
        {
            telemetrySender.TrackStartupEvent(parseResult, eventBuilder);
        }

        // If sentinel does not exist, print the welcome message showing the telemetry notification.
        if (!TelemetrySender.SkipFirstTimeExperience &&
            !telemetrySender.FirstTimeUseNoticeSentinelExists())
        {
            var output = parseResult.InvocationConfiguration.Output;

            output.WriteLine();
            output.WriteLine(TelemetrySender.WelcomeMessage);

            telemetrySender.CreateFirstTimeUseNoticeSentinelIfNotExists();
        }
    }
}