// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.App.Connection;
using Microsoft.DotNet.Interactive.App.Tests.Extensions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using CommandLineParser = Microsoft.DotNet.Interactive.App.CommandLine.CommandLineParser;

namespace Microsoft.DotNet.Interactive.App.Tests.CommandLine;

public class CommandLineParserTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private StartupOptions _startupOptions;
    private readonly RootCommand _rootCommand;
    private readonly FileInfo _connectionFile;
    private readonly DirectoryInfo _kernelSpecInstallPath;
    private readonly ServiceCollection _serviceCollection;

    public CommandLineParserTests(ITestOutputHelper output)
    {
        _output = output;
        _serviceCollection = new ServiceCollection();
        var firstTimeUseNoticeSentinel = new FakeFirstTimeUseNoticeSentinel
        {
            SentinelExists = false
        };

        _rootCommand = CommandLineParser.Create(
            _serviceCollection,
            startWebServer: startupOptions =>
            {
                _startupOptions = startupOptions;
            },
            startJupyter: (startupOptions, _) =>
            {
                _startupOptions = startupOptions;
                return Task.FromResult(1);
            },
            startStdio: (startupOptions, _) =>
            {
                _startupOptions = startupOptions;
                return Task.FromResult(1);
            },
            startHttp: (startupOptions, _) =>
            {
                _startupOptions = startupOptions;
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

        await _rootCommand.Parse($"jupyter --log-path {logPath} {_connectionFile}").InvokeAsync();

        _startupOptions
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
            kernel.AddConnectDirective(new ConnectStdIoDirective(new Uri("kernel://test-kernel")));

            string[] args = [Dotnet.Path.FullName, typeof(Program).Assembly.Location, "stdio", "--log-path", logPath.Directory.FullName, "--verbose"];

            var json = JsonSerializer.Serialize(args);

            await kernel.SendAsync(new SubmitCode($"#!connect stdio --kernel-name proxy --command {json}"));

            await kernel.SendAsync(new SubmitCode("1+1", "proxy"));
        }

        // wait for log file to be created
        var logFile = await logPath.Directory.WaitForFile(
                          timeout: waitTime,
                          predicate: _ => true); // any matching file is the one we want
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

        logFileContents.ToString().Should().Contain("[Creating kernels]  ▶");
    }

    [Fact]
    public async Task It_parses_verbose_option()
    {
        await _rootCommand.Parse($"jupyter --verbose {_connectionFile}").InvokeAsync();

        _startupOptions
            .Verbose
            .Should()
            .BeTrue();
    }

    [Fact]
    public void jupyter_command_parses_port_range_option()
    {
        _rootCommand.Parse($"jupyter --http-port-range 3000-4000 {_connectionFile}").Invoke();

        _startupOptions.HttpPortRange
                     .Should()
                     .BeEquivalentToPreferringRuntimeMemberTypes(new HttpPortRange(3000, 4000));
    }

    [Fact]
    public void jupyter_command_help_shows_default_port_range()
    {
        var output = new StringWriter();

        _rootCommand.Parse("jupyter -h").Invoke(new() { Output = output });

        output.ToString().Should().Match("*default:*2048-3000*");
    }

    [Fact]
    public void jupyter_command_parses_http_local_only_option()
    {
        _rootCommand.Parse($"jupyter --http-local-only {_connectionFile}").InvokeAsync();

        _startupOptions
            .GetAllNetworkInterfaces
            .Should()
            .Match(x => x == StartupOptions.GetNetworkInterfacesHttpLocalOnly);

        _startupOptions
            .GetAllNetworkInterfaces
            .Should()
            .Match(x => x != NetworkInterface.GetAllNetworkInterfaces);
    }

    [Fact]
    public void jupyter_command_default_network_interface_if_no_http_local_only_option()
    {
        _rootCommand.Parse($"jupyter {_connectionFile}").InvokeAsync();

        _startupOptions
            .GetAllNetworkInterfaces
            .Should()
            .Match(x => x != StartupOptions.GetNetworkInterfacesHttpLocalOnly);

        _startupOptions
            .GetAllNetworkInterfaces
            .Should()
            .Match(x => x == NetworkInterface.GetAllNetworkInterfaces);
    }

    [Fact]
    public void jupyter_install_command_parses_path_option()
    {
        Directory.CreateDirectory(_kernelSpecInstallPath.FullName);

        _rootCommand.Parse($"jupyter install --path {_kernelSpecInstallPath}").Invoke();

        var installedKernels = _kernelSpecInstallPath.GetDirectories();

        installedKernels
            .Select(d => d.Name)
            .Should()
            .BeEquivalentTo(".net-csharp", ".net-fsharp", ".net-powershell");
    }

    [Fact]
    public void jupyter_install_command_does_not_parse_http_port_option()
    {
        var result = _rootCommand.Parse("jupyter install --http-port 8000");

        result.Errors
              .Select(e => e.Message)
              .Should()
              .Contain(errorMessage => errorMessage == "Unrecognized command or argument '--http-port'.");
    }

    [Fact]
    public void jupyter_install_command_parses_port_range_option()
    {
        var result = _rootCommand.Parse("jupyter install --http-port-range 3000-4000");

        var startupOptions = StartupOptions.Parse(result);

        startupOptions
            .HttpPortRange
            .Should()
            .BeEquivalentToPreferringRuntimeMemberTypes(new HttpPortRange(3000, 4000));
    }

    [Fact]
    public void jupyter_command_returns_error_if_connection_file_path_is_not_passed()
    {
        var result = _rootCommand.Parse("jupyter");

        result.Errors.Should().Contain(e => e.Message == "Required argument missing for command: 'jupyter'.");
    }

    [Fact]
    public void jupyter_command_does_not_parse_http_port_option()
    {
        var result = _rootCommand.Parse($"jupyter {_connectionFile} --http-port 8000");

        result.Errors
              .Select(e => e.Message)
              .Should()
              .Contain(errorMessage => errorMessage == "Unrecognized command or argument '--http-port'.");
    }

    [Fact]
    public async Task jupyter_command_enables_http_api_when_http_port_range_is_specified()
    {
        await _rootCommand.Parse($"jupyter --http-port-range 3000-5000 {_connectionFile}").InvokeAsync();

        _startupOptions.EnableHttpApi.Should().BeTrue();
    }

    [Fact]
    public void jupyter_command_parses_connection_file_path()
    {
        _rootCommand.Parse($"jupyter {_connectionFile}").Invoke();

        _startupOptions
            .JupyterConnectionFile
            .FullName
            .Should()
            .Be(_connectionFile.FullName);
    }

    [Fact]
    public async Task jupyter_command_enables_http_api_by_default()
    {
        await _rootCommand.Parse($"jupyter {_connectionFile}").InvokeAsync();

        _startupOptions.EnableHttpApi.Should().BeTrue();
    }

    [Fact]
    public async Task jupyter_command_by_default_uses_port_rage()
    {
        await _rootCommand.Parse($"jupyter {_connectionFile}").InvokeAsync();

        using var scope = new AssertionScope();
        _startupOptions.HttpPortRange.Should().NotBeNull();
        _startupOptions.HttpPortRange.Start.Should().Be(HttpPortRange.Default.Start);
        _startupOptions.HttpPortRange.End.Should().Be(HttpPortRange.Default.End);
    }

    [Fact]
    public void jupyter_command_default_kernel_option_value()
    {
        _rootCommand.Parse($"jupyter {Path.GetTempFileName()}").Invoke();

        _startupOptions.DefaultKernel.Should().Be("csharp");
    }

    [Fact]
    public void jupyter_command_honors_default_kernel_option()
    {
        _rootCommand.Parse($"jupyter --default-kernel bsharp {Path.GetTempFileName()}").Invoke();

        _startupOptions.DefaultKernel.Should().Be("bsharp");
    }

    [Fact]
    public async Task jupyter_command_returns_error_if_connection_file_path_does_not_exist()
    {
        var expected = "not_exist.json";

        var error = new StringWriter();
        await _rootCommand.Parse($"jupyter {expected}").InvokeAsync(new() { Output = new StringWriter(), Error = error });

        error.ToString().Should().ContainAll("File does not exist", "not_exist.json");
    }

    [Fact]
    public void stdio_command_kernel_host_defaults_to_process_id()
    {
        _rootCommand.Parse("stdio").Invoke();

        // FIX: (stdio_command_kernel_host_defaults_to_process_id) maybe broken because Uri parsing was removed?

        _startupOptions.KernelHostUri
                     .Should()
                     .Be(new Uri($"kernel://pid-{Process.GetCurrentProcess().Id}"));
    }

    [Fact]
    public void stdio_command_kernel_host_uri_can_be_specified()
    {
        _rootCommand.Parse("stdio --kernel-host some-kernel-name").Invoke();
        ;

        _startupOptions.KernelHostUri
                     .Should()
                     .Be(new Uri("kernel://some-kernel-name"));
    }

    [Fact]
    public void stdio_command_working_dir_defaults_to_process_current()
    {
        _rootCommand.Parse("stdio").Invoke();

        _startupOptions.WorkingDir.FullName
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

        var result = _rootCommand.Parse($"stdio --working-dir {workingDir}");

        var startupOptions = StartupOptions.Parse(result);

        startupOptions.WorkingDir.FullName
                      .Should()
                      .Be(workingDir);
    }

    [Fact]
    public void stdio_command_does_not_support_http_port_and_http_port_range_options_at_same_time()
    {
        var result = _rootCommand.Parse("stdio --http-port 8000 --http-port-range 3000-4000");

        result.Errors
              .Select(e => e.Message)
              .Should()
              .Contain(errorMessage => errorMessage == "Cannot specify both --http-port-range and --http-port together");
    }

    [Fact]
    public void stdio_command_parses_http_port_options()
    {
        _rootCommand.Parse("stdio --http-port 8000").Invoke();

        _startupOptions.HttpPort.PortNumber.Should().Be(8000);
    }

    [Fact]
    public async Task stdio_command_parses_http_port_range_options()
    {
        await _rootCommand.Parse("stdio --http-port-range 3000-4000").InvokeAsync();

        using var scope = new AssertionScope();
        _startupOptions.HttpPortRange.Should().NotBeNull();
        _startupOptions.HttpPortRange.Start.Should().Be(3000);
        _startupOptions.HttpPortRange.End.Should().Be(4000);
    }

    [Fact]
    public async Task stdio_command_requires_api_bootstrapping_when_http_is_enabled()
    {
        await _rootCommand.Parse("stdio --http-port-range 3000-4000").InvokeAsync();

        var kernel = GetKernel();

        kernel.FrontendEnvironment.As<HtmlNotebookFrontendEnvironment>()
              .RequiresAutomaticBootstrapping
              .Should()
              .BeTrue();
    }

    [Fact]
    public void stdio_command_defaults_to_csharp_kernel()
    {
        _rootCommand.Parse("stdio").Invoke();

        _startupOptions.DefaultKernel.Should().Be("csharp");
    }

    [Fact]
    public async Task stdio_command_does_not_enable_http_api_by_default()
    {
        // FIX: (stdio_command_does_not_enable_http_api_by_default) inline
        var parseResult = _rootCommand.Parse("stdio");
        parseResult.Errors.Should().BeEmpty();

        await parseResult.InvokeAsync();

        _startupOptions.EnableHttpApi.Should().BeFalse();
    }

    [Fact]
    public void stdio_command_honors_default_kernel_option()
    {
        _rootCommand.Parse("stdio --default-kernel bsharp").Invoke();

        _startupOptions.DefaultKernel.Should().Be("bsharp");
    }

    [Fact]
    public void Parser_configuration_is_valid()
    {
        _rootCommand.ThrowIfInvalid();
    }
}