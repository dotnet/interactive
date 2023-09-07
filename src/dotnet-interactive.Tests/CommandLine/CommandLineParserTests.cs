// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;
using FluentAssertions.Execution;

using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.App.Connection;
using Microsoft.DotNet.Interactive.App.Tests.Extensions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.Extensions.DependencyInjection;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.App.Tests.CommandLine;

public class CommandLineParserTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestConsole _console = new();
    private StartupOptions _startOptions;
    private readonly Parser _parser;
    private readonly FileInfo _connectionFile;
    private readonly DirectoryInfo _kernelSpecInstallPath;
    private readonly ServiceCollection _serviceCollection;

    public CommandLineParserTests(ITestOutputHelper output)
    {
        KernelCommandEnvelope.RegisterDefaults();
        KernelEventEnvelope.RegisterDefaults();

        _output = output;
        _serviceCollection = new ServiceCollection();
        var firstTimeUseNoticeSentinel = new FakeFirstTimeUseNoticeSentinel
        {
            SentinelExists = false
        };

        _parser = CommandLineParser.Create(
            _serviceCollection,
            startServer: (options, invocationContext) =>
            {
                _startOptions = options;
            },
            jupyter: (startupOptions, console, startServer, context) =>
            {
                _startOptions = startupOptions;
                return Task.FromResult(1);
            },
            startKernelHost: (startupOptions, host, console) =>
            {
                _startOptions = startupOptions;
                return Task.FromResult(1);
            },
            startHttp: (startupOptions, console, startServer, context) =>
            {
                _startOptions = startupOptions;
                return Task.FromResult(1);
            },
            telemetrySender: new FakeTelemetrySender(firstTimeUseNoticeSentinel));

        _connectionFile = new FileInfo(Path.GetTempFileName());
        _kernelSpecInstallPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
    }

    private Kernel GetKernel()
    {
        return _serviceCollection
            .FirstOrDefault(s => s.ServiceType == typeof(Kernel))
            .ImplementationInstance.As<Kernel>();
    }

    public void Dispose()
    {
        _connectionFile.Delete();
    }

    [Fact]
    public async Task It_parses_log_output_directory()
    {
        var logPath = new DirectoryInfo(Path.GetTempPath());

        await _parser.InvokeAsync($"jupyter --log-path {logPath} {_connectionFile}", _console);

        _startOptions
            .LogPath
            .FullName
            .Should()
            .Be(logPath.FullName);
    }

    [Fact]
    public async Task stdio_mode_honors_log_path()
    {
        using var logPath = DisposableDirectory.Create();

        _output.WriteLine($"Created log file: {logPath.Directory.FullName}");

        var waitTime = TimeSpan.FromSeconds(10);

        using (var kernel = new CompositeKernel())
        {
            kernel.AddKernelConnector(new ConnectStdIoCommand(new Uri("kernel://test-kernel")));

            await kernel.SendAsync(new SubmitCode($"#!connect stdio --kernel-name proxy --command \"{Dotnet.Path}\" \"{typeof(Program).Assembly.Location}\" stdio --log-path \"{logPath.Directory.FullName}\" --verbose"));

            await kernel.SendAsync(new SubmitCode("1+1", "proxy"));
        }

        // wait for log file to be created
        var logFile = await logPath.Directory.WaitForFile(
            timeout: waitTime,
            predicate: _file => true); // any matching file is the one we want
        logFile.Should().NotBeNull($"a log file should have been created at {logFile.FullName}");

        // check log file for expected contents
        (await logFile.WaitForFileCondition(
             timeout: waitTime,
             predicate: file => file.Length > 0))
            .Should()
            .BeTrue($"expected non-empty log file within {waitTime.TotalSeconds}s");

        var logFileContents = new StringBuilder();

        await using var fileStream = new FileStream(logFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var fileReader = new StreamReader(fileStream);

        while (!fileReader.EndOfStream)
        {
            var line = await fileReader.ReadLineAsync();
            logFileContents.Append(line);
        }

        logFileContents.ToString().Should().Contain("[KernelInvocationContext]  ▶  +[ ⁞Ϲ⁞ SubmitCode 1+1");
    }

    [Fact]
    public async Task It_parses_verbose_option()
    {
        await _parser.InvokeAsync($"jupyter --verbose {_connectionFile}", _console);

        _startOptions
            .Verbose
            .Should()
            .BeTrue();
    }

    [Fact]
    public void jupyter_command_parses_port_range_option()
    {
        var result = _parser.Parse($"jupyter --http-port-range 3000-4000 {_connectionFile}");

        var binder = new ModelBinder<StartupOptions>();

        var options = (StartupOptions)binder.CreateInstance(new InvocationContext(result).BindingContext);

        options
            .HttpPortRange
            .Should()
            .BeEquivalentToRespectingRuntimeTypes(new HttpPortRange(3000, 4000));
    }

    [Fact]
    public void jupyter_command_help_shows_default_port_range()
    {
        _parser.Invoke("jupyter -h", _console);

        _console.Out.ToString().Should().Contain("default: 2048-3000");
    }

    [Fact]
    public void jupyter_install_command_parses_path_option()
    {
        Directory.CreateDirectory(_kernelSpecInstallPath.FullName);

        _parser.InvokeAsync($"jupyter install --path {_kernelSpecInstallPath}");

        var installedKernels = _kernelSpecInstallPath.GetDirectories();

        installedKernels
            .Select(d => d.Name)
            .Should()
            .BeEquivalentTo(".net-csharp", ".net-fsharp", ".net-powershell");
    }

    [Fact]
    public void jupyter_install_command_does_not_parse_http_port_option()
    {
        var result = _parser.Parse("jupyter install --http-port 8000");

        result.Errors
            .Select(e => e.Message)
            .Should()
            .Contain(errorMessage => errorMessage == "Unrecognized command or argument '--http-port'.");
    }

    [Fact]
    public void jupyter_install_command_parses_port_range_option()
    {
        var result = _parser.Parse("jupyter install --http-port-range 3000-4000");

        var binder = new ModelBinder<StartupOptions>();

        var options = (StartupOptions)binder.CreateInstance(new InvocationContext(result).BindingContext);

        options
            .HttpPortRange
            .Should()
            .BeEquivalentToRespectingRuntimeTypes(new HttpPortRange(3000, 4000));
    }

    [Fact]
    public async Task jupyter_command_returns_error_if_connection_file_path_is_not_passed()
    {
        var testConsole = new TestConsole();

        await _parser.InvokeAsync("jupyter", testConsole);

        testConsole.Error.ToString().Should().Contain("Required argument missing for command: 'jupyter'.");
    }

    [Fact]
    public void jupyter_command_does_not_parse_http_port_option()
    {
        var result = _parser.Parse($"jupyter {_connectionFile} --http-port 8000");

        result.Errors
            .Select(e => e.Message)
            .Should()
            .Contain(errorMessage => errorMessage == "Unrecognized command or argument '--http-port'.");
    }

    [Fact]
    public async Task jupyter_command_enables_http_api_when_http_port_range_is_specified()
    {
        await _parser.InvokeAsync($"jupyter --http-port-range 3000-5000 {_connectionFile}");

        _startOptions.EnableHttpApi.Should().BeTrue();
    }

    [Fact]
    public void jupyter_command_parses_connection_file_path()
    {
        var result = _parser.Parse($"jupyter {_connectionFile}");

        var binder = new ModelBinder<JupyterOptions>();

        var options = (JupyterOptions)binder.CreateInstance(new InvocationContext(result).BindingContext);

        options
            .ConnectionFile
            .FullName
            .Should()
            .Be(_connectionFile.FullName);
    }

    [Fact]
    public async Task jupyter_command_enables_http_api_by_default()
    {
        await _parser.InvokeAsync($"jupyter {_connectionFile}");

        _startOptions.EnableHttpApi.Should().BeTrue();
    }

    [Fact]
    public async Task jupyter_command_by_default_uses_port_rage()
    {
        await _parser.InvokeAsync($"jupyter {_connectionFile}");

        using var scope = new AssertionScope();
        _startOptions.HttpPortRange.Should().NotBeNull();
        _startOptions.HttpPortRange.Start.Should().Be(HttpPortRange.Default.Start);
        _startOptions.HttpPortRange.End.Should().Be(HttpPortRange.Default.End);
    }

    [Fact]
    public void jupyter_command_default_kernel_option_value()
    {
        var result = _parser.Parse($"jupyter {Path.GetTempFileName()}");
        var binder = new ModelBinder<JupyterOptions>();
        var options = (JupyterOptions)binder.CreateInstance(new InvocationContext(result).BindingContext);

        options.DefaultKernel.Should().Be("csharp");
    }

    [Fact]
    public void jupyter_command_honors_default_kernel_option()
    {
        var result = _parser.Parse($"jupyter --default-kernel bsharp {Path.GetTempFileName()}");
        var binder = new ModelBinder<JupyterOptions>();
        var options = (JupyterOptions)binder.CreateInstance(new InvocationContext(result).BindingContext);

        options.DefaultKernel.Should().Be("bsharp");
    }

    [Fact]
    public async Task jupyter_command_returns_error_if_connection_file_path_does_not_exist()
    {
        var expected = "not_exist.json";

        var testConsole = new TestConsole();
        await _parser.InvokeAsync($"jupyter {expected}", testConsole);

        testConsole.Error.ToString().Should().ContainAll("File does not exist", "not_exist.json");
    }

    [Fact]
    public void stdio_command_kernel_host_defaults_to_process_id()
    {
        var result = _parser.Parse("stdio");

        var binder = new ModelBinder<StartupOptions>();

        var options = (StartupOptions)binder.CreateInstance(new InvocationContext(result).BindingContext);

        options.KernelHost
            .Should()
            .Be(new Uri($"kernel://pid-{Process.GetCurrentProcess().Id}"));
    }

    [Fact]
    public void stdio_command_kernel_name_can_be_specified()
    {
        var result = _parser.Parse("stdio --kernel-host some-kernel-name");

        var binder = new ModelBinder<StartupOptions>();

        var options = (StartupOptions)binder.CreateInstance(new InvocationContext(result).BindingContext);

        options.KernelHost
            .Should()
            .Be(new Uri("kernel://some-kernel-name"));
    }

    [Fact]
    public void stdio_command_working_dir_defaults_to_process_current()
    {
        var result = _parser.Parse("stdio");

        var binder = new ModelBinder<StartupOptions>();

        var options = (StartupOptions)binder.CreateInstance(new InvocationContext(result).BindingContext);

        options.WorkingDir.FullName
            .Should()
            .Be(Environment.CurrentDirectory);
    }

    [Fact]
    public void stdio_command_working_dir_can_be_specified()
    {
        // StartupOptions.WorkingDir is of type DirectoryInfo which normalizes paths to OS type and ensures that
        // they're rooted.  To ensure proper testing behavior we have to give an os-specific path.
        var workingDir = Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => "C:\\some\\dir",
            _ => "/some/dir"
        };

        var result = _parser.Parse($"stdio --working-dir {workingDir}");

        var binder = new ModelBinder<StartupOptions>();

        var options = (StartupOptions)binder.CreateInstance(new InvocationContext(result).BindingContext);

        options.WorkingDir.FullName
            .Should()
            .Be(workingDir);
    }


    [Fact]
    public void stdio_command_does_not_support_http_port_and_http_port_range_options_at_same_time()
    {
        var result = _parser.Parse("stdio --http-port 8000 --http-port-range 3000-4000");

        result.Errors
            .Select(e => e.Message)
            .Should()
            .Contain(errorMessage => errorMessage == "Cannot specify both --http-port-range and --http-port together");
    }

    [Fact]
    public void stdio_command_parses_http_port_options()
    {
        var result = _parser.Parse("stdio --http-port 8000");

        var binder = new ModelBinder<StartupOptions>();

        var options = (StartupOptions)binder.CreateInstance(new InvocationContext(result).BindingContext);

        options.HttpPort.PortNumber.Should().Be(8000);
    }

    [Fact]
    public async Task stdio_command_parses_http_port_range_options()
    {
        await _parser.InvokeAsync("stdio --http-port-range 3000-4000");

        using var scope = new AssertionScope();
        _startOptions.HttpPortRange.Should().NotBeNull();
        _startOptions.HttpPortRange.Start.Should().Be(3000);
        _startOptions.HttpPortRange.End.Should().Be(4000);
    }

    [Fact]
    public async Task stdio_command_requires_api_bootstrapping_when_http_is_enabled()
    {
        await _parser.InvokeAsync("stdio --http-port-range 3000-4000");

        var kernel = GetKernel();

        kernel.FrontendEnvironment.As<HtmlNotebookFrontendEnvironment>()
            .RequiresAutomaticBootstrapping
            .Should()
            .BeTrue();
    }

    [Fact]
    public void stdio_command_defaults_to_csharp_kernel()
    {
        var result = _parser.Parse("stdio");
        var binder = new ModelBinder<StdIOOptions>();
        var options = (StdIOOptions)binder.CreateInstance(new InvocationContext(result).BindingContext);

        options.DefaultKernel.Should().Be("csharp");
    }

    [Fact]
    public async Task stdio_command_does_not_enable_http_api_by_default()
    {
        await _parser.InvokeAsync("stdio");

        _startOptions.EnableHttpApi.Should().BeFalse();
    }


    [Fact]
    public void stdio_command_honors_default_kernel_option()
    {
        var result = _parser.Parse("stdio --default-kernel bsharp");
        var binder = new ModelBinder<StdIOOptions>();
        var options = (StdIOOptions)binder.CreateInstance(new InvocationContext(result).BindingContext);

        options.DefaultKernel.Should().Be("bsharp");
    }

    [Fact]
    public void Parser_configuration_is_valid()
    {
        _parser.Configuration.ThrowIfInvalid();
    }
}