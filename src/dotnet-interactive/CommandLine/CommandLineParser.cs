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
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.App.Connection;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Documents.ParserServer;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.Csv;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Jupyter.Formatting;
using Microsoft.DotNet.Interactive.Mermaid;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.DotNet.Interactive.VSCode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pocket;
using static Pocket.Logger;

using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;

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
        NotebookParserServer notebookParserServer);

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

        startKernelHost ??= KernelHostLauncher.Do;

        startNotebookParser ??= ParseNotebookCommand.Do;

        startHttp ??= HttpCommand.Do;

        // Setup first time use notice sentinel.
        var buildInfo = BuildInfo.GetBuildInfo(typeof(Program).Assembly);

        // Setup telemetry.
        telemetrySender ??= new TelemetrySender(
            buildInfo.AssemblyInformationalVersion,
            new FirstTimeUseNoticeSentinel(buildInfo.AssemblyInformationalVersion));

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
            getDefaultValue: () => "csharp").AddCompletions("fsharp", "csharp", "pwsh");

        var rootCommand = DotnetInteractive();

        rootCommand.AddCommand(Jupyter());
        rootCommand.AddCommand(StdIO());
        rootCommand.AddCommand(NotebookParser());

        var filter = new StartupTelemetryEventBuilder(Sha256Hasher.ToSha256HashWithNormalizedCasing);

        return new CommandLineBuilder(rootCommand)
            .UseDefaults()
            .CancelOnProcessTermination()
            .AddMiddleware(async (context, next) =>
            {
                if (context.ParseResult.Errors.Count == 0)
                {
                    telemetrySender.TrackStartupEvent(context.ParseResult, filter);
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

            var jupyterCommand = new Command("jupyter", "Starts dotnet-interactive as a Jupyter kernel")
            {
                defaultKernelOption,
                httpPortRangeOption,
                new Argument<FileInfo>
                {
                    Name = "connection-file",
                    Description = "The path to a connection file provided by Jupyter"
                }.ExistingOnly()
            };

            jupyterCommand.Handler = CommandHandler.Create<StartupOptions, JupyterOptions, IConsole, InvocationContext, CancellationToken>(JupyterHandler);

            var installCommand = new Command("install", "Install the .NET kernel for Jupyter")
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
                var kernel = CreateKernel(options.DefaultKernel, frontendEnvironment, startupOptions, telemetrySender);
                cancellationToken.Register(() => kernel.Dispose());
                
                await new JupyterClientKernelExtension().OnLoadAsync(kernel);

                services.AddKernel(kernel);

                var clientSideKernelClient = new SignalRBackchannelKernelClient();

                services.AddSingleton(_ => ConnectionInformation.Load(options.ConnectionFile))
                    .AddSingleton(clientSideKernelClient)
                    .AddSingleton(c =>
                    {
                        return new JupyterRequestContextScheduler(delivery => c.GetRequiredService<JupyterRequestContextHandler>()
                            .Handle(delivery));
                    })
                    .AddSingleton(c => new JupyterRequestContextHandler(kernel))
                    .AddSingleton<IHostedService, Shell>()
                    .AddSingleton<IHostedService, Heartbeat>();

                var result = await jupyter(startupOptions, console, startServer, context);
                
                return result;
            }

            Task<int> JupyterInstallHandler(HttpPortRange httpPortRange, DirectoryInfo path, InvocationContext context)
            {
                var jupyterInstallCommand = new JupyterInstallCommand(context.Console, new JupyterKernelSpecInstaller(context.Console), httpPortRange, path);
                return jupyterInstallCommand.InvokeAsync();
            }
        }

        Command StdIO()
        {
            var httpPortRangeOption = new Option<HttpPortRange>(
                "--http-port-range",
                parseArgument: result => result.Tokens.Count == 0 ? HttpPortRange.Default : ParsePortRangeOption(result),
                description: "Specifies the range of ports to use to enable HTTP services");

            var httpPortOption = new Option<HttpPort>(
                "--http-port",
                description: "Specifies the port on which to enable HTTP services",
                parseArgument: result =>
                {
                    if (result.FindResultFor(httpPortRangeOption) is { } conflictingOption)
                    {
                        var parsed = result.Parent as OptionResult;
                        result.ErrorMessage = $"Cannot specify both {conflictingOption.Token.Value} and {parsed.Token.Value} together";
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
                        result.ErrorMessage = "Must specify a port number or *.";
                        return null;
                    }

                    return new HttpPort(portNumber);
                });

            var kernelHostOption = new Option<Uri>(
                "--kernel-host",
                parseArgument: x => x.Tokens.Count == 0 ? KernelHost.CreateHostUriForCurrentProcessId() : KernelHost.CreateHostUri(x.Tokens[0].Value),
                isDefault: true,
                description: "Name of the kernel host.");

            var previewOption = new Option<bool>("--preview", description: "Enable preview kernel features.");

            var workingDirOption = new Option<DirectoryInfo>(
                "--working-dir",
                () => new DirectoryInfo(Environment.CurrentDirectory),
                "Working directory to which to change after launching the kernel.");

            var stdIOCommand = new Command(
                "stdio",
                "Starts dotnet-interactive with kernel functionality exposed over standard I/O")
            {
                defaultKernelOption,
                httpPortRangeOption,
                httpPortOption,
                kernelHostOption,
                previewOption,
                workingDirOption
            };

            stdIOCommand.Handler = CommandHandler.Create<StartupOptions, StdIOOptions, IConsole, InvocationContext,CancellationToken>(
                async (startupOptions, options, console, context, cancellationToken) =>
                {
                    Console.InputEncoding = Encoding.UTF8;
                    Console.OutputEncoding = Encoding.UTF8;
                    Environment.CurrentDirectory = startupOptions.WorkingDir.FullName;

                    FrontendEnvironment frontendEnvironment = startupOptions.EnableHttpApi
                        ? new HtmlNotebookFrontendEnvironment()
                        : new BrowserFrontendEnvironment();

                    var kernel = CreateKernel(
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
                        var vscodeSetup = new VSCodeClientKernelExtension();
                        await vscodeSetup.OnLoadAsync(kernel);
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
                        if (!isVSCode)
                        {
                            var proxy = await host.ConnectProxyKernelOnDefaultConnectorAsync("javascript", new Uri("kernel://webview/javascript"));

                            proxy.KernelInfo.SupportedKernelCommands.Add(new(nameof(SubmitCode)));
                        }
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
                "Starts a process to parse and serialize notebooks.");
            notebookParserCommand.Handler = CommandHandler.Create(async () =>
            {
                Console.InputEncoding = Encoding.UTF8;
                Console.OutputEncoding = Encoding.UTF8;
                var notebookParserServer = new NotebookParserServer(Console.In, Console.Out);
                await startNotebookParser(notebookParserServer);
            });
            return notebookParserCommand;
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

    private static CompositeKernel CreateKernel(string defaultKernelName, FrontendEnvironment frontendEnvironment, StartupOptions startupOptions, TelemetrySender telemetrySender)
    {
        using var _ = Log.OnEnterAndExit("Creating Kernels");

        var compositeKernel = new CompositeKernel();
        compositeKernel.FrontendEnvironment = frontendEnvironment;

        // TODO: temporary measure to support vscode integrations
        compositeKernel.Add(new SqlDiscoverabilityKernel());
        compositeKernel.Add(new KqlDiscoverabilityKernel());

        compositeKernel.Add(
            new CSharpKernel()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseWho()
                .UseMathAndLaTeX()
                .UseValueSharing(),
            new[] { "c#", "C#" });

        compositeKernel.Add(
            new FSharpKernel()
                .UseDefaultFormatting()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseWho()
                .UseMathAndLaTeX()
                .UseValueSharing(),
            new[] { "f#", "F#" });

        compositeKernel.Add(
            new PowerShellKernel()
                .UseProfiles()
                .UseValueSharing(),
            new[] { "powershell" });

        compositeKernel.Add(
            new HtmlKernel());

        compositeKernel.Add(
            new KeyValueStoreKernel()
                .UseWho());

        compositeKernel.Add(
            new MermaidKernel());

        var kernel = compositeKernel
            .UseDefaultMagicCommands()
            .UseLogMagicCommand()
            .UseAboutMagicCommand()
            .UseImportMagicCommand()
            .UseNuGetExtensions();

        kernel.AddKernelConnector(new ConnectNamedPipeCommand());
        kernel.AddKernelConnector(new ConnectSignalRCommand());
        kernel.AddKernelConnector(new ConnectStdIoCommand(startupOptions.KernelHost));

        if (startupOptions.Verbose)
        {
            kernel.LogEventsToPocketLogger();
        }

        SetUpFormatters(frontendEnvironment);

        kernel.DefaultKernelName = defaultKernelName;

        kernel.UseTelemetrySender(telemetrySender);

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
                Formatter.SetPreferredMimeTypesFor(typeof(LaTeXString), "text/latex");
                Formatter.SetPreferredMimeTypesFor(typeof(MathString), "text/latex");
                Formatter.SetPreferredMimeTypesFor(typeof(string), PlainTextFormatter.MimeType);
                Formatter.SetPreferredMimeTypesFor(typeof(ScriptContent), HtmlFormatter.MimeType);
                Formatter.SetPreferredMimeTypesFor(typeof(TabularDataResource), HtmlFormatter.MimeType, CsvFormatter.MimeType);

                Formatter.Register<LaTeXString>((laTeX, writer) => writer.Write(laTeX.ToString()), "text/latex");
                Formatter.Register<MathString>((math, writer) => writer.Write(math.ToString()), "text/latex");
                Formatter.Register<ScriptContent>((script, writer) =>
                {
                    IHtmlContent content =
                        PocketViewTags.script[type: "text/javascript"](script.ScriptValue.ToHtmlContent());
                    content.WriteTo(writer, HtmlEncoder.Default);
                }, HtmlFormatter.MimeType);

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(frontendEnvironment));
        }

      
    }
}