// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.Binding;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.App.Tests.CommandLine
{
    public class CommandLineParserTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly TestConsole _console = new TestConsole();
        private StartupOptions _startOptions;
        private readonly Parser _parser;
        private readonly FileInfo _connectionFile;
        private DirectoryInfo _kernelSpecInstallPath;

        public CommandLineParserTests(ITestOutputHelper output)
        {
            _output = output;

            _parser = CommandLineParser.Create(
                new ServiceCollection(),
                startServer: (options, invocationContext) =>
                {
                    _startOptions = options;
                },
                jupyter: (startupOptions, console, startServer, context) =>
                {
                    _startOptions = startupOptions;
                    return Task.FromResult(1);
                },
                startStdIO: (startupOptions, kernel, console) =>
                {
                    _startOptions = startupOptions;
                    return Task.FromResult(1);
                },
                startHttp: (startupOptions, console, startServer, context) =>
                {
                    _startOptions = startupOptions;
                    return Task.FromResult(1);
                },
            telemetry: new FakeTelemetry(),
                firstTimeUseNoticeSentinel: new NopFirstTimeUseNoticeSentinel());

            _connectionFile = new FileInfo(Path.GetTempFileName());
            _kernelSpecInstallPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
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

            var options = (StartupOptions)binder.CreateInstance(new BindingContext(result));

            options
                .HttpPortRange
                .Should()
                .BeEquivalentTo(new HttpPortRange(3000, 4000));
        }

        [Fact]
        public void jupyter_install_command_parses_path_option()
        {
            Directory.CreateDirectory(_kernelSpecInstallPath.FullName);
            
            var result = _parser.InvokeAsync($"jupyter install --path {_kernelSpecInstallPath}");

            var installedKernels = _kernelSpecInstallPath.GetDirectories();

            installedKernels
                .Select(d => d.Name)
                .Should()
                .BeEquivalentTo(".net-csharp", ".net-fsharp", ".net-powershell");
        }

        [Fact]
        public void jupyter_install_command_does_not_parse_http_port_option()
        {
            var result = _parser.Parse($"jupyter install --http-port 8000");

            result.Errors
                .Select(e => e.Message)
                .Should()
                .Contain(errorMessage => errorMessage == "Unrecognized command or argument '--http-port'");
        }

        [Fact]
        public void jupyter_install_command_parses_port_range_option()
        {
            var result = _parser.Parse("jupyter install --http-port-range 3000-4000");

            var binder = new ModelBinder<StartupOptions>();

            var options = (StartupOptions)binder.CreateInstance(new BindingContext(result));

            options
                .HttpPortRange
                .Should()
                .BeEquivalentTo(new HttpPortRange(3000, 4000));
        }

        [Fact]
        public async Task http_command_enables_http_api_by_default()
        {
            await _parser.InvokeAsync($"http");

            _startOptions.EnableHttpApi.Should().BeTrue();
        }

        [Fact]
        public async Task http_command_uses_default_port()
        {
            await _parser.InvokeAsync($"http");

            using var scope = new AssertionScope();
            _startOptions.HttpPort.Should().NotBeNull();
            _startOptions.HttpPort.IsAuto.Should().BeTrue();
        }

        [Fact]
        public void http_command__does_not_parse_http_port_range_option()
        {
            var result = _parser.Parse("http --http-port-range 6000-10000");

            result.Errors
                .Select(e => e.Message)
                 .Should()
                 .Contain(errorMessage => errorMessage == "Unrecognized command or argument '--http-port-range'");
        }

        [Fact]
        public async Task jupyter_command_returns_error_if_connection_file_path_is_not_passed()
        {
            var testConsole = new TestConsole();
            await _parser.InvokeAsync("jupyter", testConsole);

            testConsole.Error.ToString().Should().Contain("Required argument missing for command: jupyter");
        }

        [Fact]
        public void jupyter_command_does_not_parse_http_port_option()
        {
            var result = _parser.Parse($"jupyter {_connectionFile} --http-port 8000");

            result.Errors
                .Select(e => e.Message)
                .Should()
                .Contain(errorMessage => errorMessage == "Unrecognized command or argument '--http-port'");
        }

        [Fact]
        public async Task jupyter_command_enables_http_api_when_http_port_range_is_specified()
        {
            await  _parser.InvokeAsync($"jupyter --http-port-range 3000-5000 {_connectionFile}");

            _startOptions.EnableHttpApi.Should().BeTrue();
        }

        [Fact]
        public void jupyter_command_parses_connection_file_path()
        {
            var result = _parser.Parse($"jupyter {_connectionFile}");

            var binder = new ModelBinder<JupyterOptions>();

            var options = (JupyterOptions)binder.CreateInstance(new BindingContext(result));

            options
                .ConnectionFile
                .FullName
                .Should()
                .Be(_connectionFile.FullName);
        }

        [Fact]
        public async Task jupyter_command_does_enable_http_api_by_default()
        {
            await  _parser.InvokeAsync($"jupyter {_connectionFile}");

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
            var options = (JupyterOptions)binder.CreateInstance(new BindingContext(result));

            options.DefaultKernel.Should().Be("csharp");
        }

        [Fact]
        public void jupyter_command_honors_default_kernel_option()
        {
            var result = _parser.Parse($"jupyter --default-kernel bsharp {Path.GetTempFileName()}");
            var binder = new ModelBinder<JupyterOptions>();
            var options = (JupyterOptions)binder.CreateInstance(new BindingContext(result));

            options.DefaultKernel.Should().Be("bsharp");
        }

        [Fact]
        public async Task jupyter_command_returns_error_if_connection_file_path_does_not_exits()
        {
            var expected = "not_exist.json";

            var testConsole = new TestConsole();
            await _parser.InvokeAsync($"jupyter {expected}", testConsole);

            testConsole.Error.ToString().Should().Contain("File does not exist: not_exist.json");
        }

        [Theory]
        [InlineData("stdio --http-port 8000", "Unrecognized command or argument '--http-port'")]
        [InlineData("stdio --http-port-range 3000-4000", "Unrecognized command or argument '--http-port-range'")]
        public void stdio_command_does_not_support_http_options(string commandLine, string expectedError)
        {
            var result = _parser.Parse(commandLine);

            result.Errors
                .Select(e => e.Message)
                .Should()
                .Contain(errorMessage => errorMessage == expectedError);
        }

        [Fact]
        public void stdio_command_defaults_to_csharp_kernel()
        {
            var result = _parser.Parse("stdio");
            var binder = new ModelBinder<StdIOOptions>();
            var options = (StdIOOptions)binder.CreateInstance(new BindingContext(result));

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
            var options = (StdIOOptions)binder.CreateInstance(new BindingContext(result));

            options.DefaultKernel.Should().Be("bsharp");
        }

       
    }
}
