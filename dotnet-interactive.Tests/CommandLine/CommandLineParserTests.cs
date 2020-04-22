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
                .BeEquivalentTo(new PortRange(3000, 4000));
        }

        [Fact]
        public void jupyter_install_command_parses_path_option()
        {
            Directory.CreateDirectory(_kernelSpecInstallPath.FullName);
            
            var result = _parser.Parse($"jupyter install --path {_kernelSpecInstallPath}");

            var option = result.CommandResult.OptionResult("--path");

            using var scope = new AssertionScope();

            option.Should().NotBeNull();
            result.FindResultFor(option.Option).GetValueOrDefault<DirectoryInfo>().FullName.Should().Be(_kernelSpecInstallPath.FullName);
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
                .BeEquivalentTo(new PortRange(3000, 4000));
        }

        [Theory]
        [InlineData("stdio --http-port 8000 --http-port-range 3000-4000")]
        [InlineData("stdio --http-port-range 3000-4000 --http-port 8000")]
        [InlineData("http --http-port 8000 --http-port-range 3000-4000")]
        [InlineData("http --http-port-range 3000-4000 --http-port 8000")]
        public void port_range_and_port_cannot_be_specified_together(string commandLine)
        {
            var result = _parser.Parse(commandLine);

            result.Errors
                .Select(e => e.Message)
                 .Should()
                 .Contain(errorMessage => errorMessage == "Cannot specify both --http-port and --http-port-range together");
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
        public void jupyter_parses_connection_file_path()
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
        public void jupyter_does_not_enable_http_api_by_default()
        {
            var result = _parser.Parse($"jupyter {_connectionFile}");

            var binder = new ModelBinder<StartupOptions>();

            var options = (StartupOptions)binder.CreateInstance(new BindingContext(result));

            options.EnableHttpApi.Should().BeFalse();
        }

        [Fact]
        public void jupyter_default_kernel_option_value()
        {
            var result = _parser.Parse($"jupyter {Path.GetTempFileName()}");
            var binder = new ModelBinder<JupyterOptions>();
            var options = (JupyterOptions)binder.CreateInstance(new BindingContext(result));
            options.DefaultKernel.Should().Be("csharp");
        }

        [Fact]
        public void jupyter_honors_default_kernel_option()
        {
            var result = _parser.Parse($"jupyter --default-kernel bsharp {Path.GetTempFileName()}");
            var binder = new ModelBinder<JupyterOptions>();
            var options = (JupyterOptions)binder.CreateInstance(new BindingContext(result));
            options.DefaultKernel.Should().Be("bsharp");
        }

        [Fact]
        public async Task jupyter_returns_error_if_connection_file_path_does_not_exits()
        {
            var expected = "not_exist.json";

            var testConsole = new TestConsole();
            await _parser.InvokeAsync($"jupyter {expected}", testConsole);

            testConsole.Error.ToString().Should().Contain("File does not exist: not_exist.json");
        }

        [Fact]
        public void stdio_command_defaults_to_csharp_kernel()
        {
            var result = _parser.Parse("stdio");
            var binder = new ModelBinder<KernelServerOptions>();
            var options = (KernelServerOptions)binder.CreateInstance(new BindingContext(result));
            options.DefaultKernel.Should().Be("csharp");
        }

        [Fact]
        public void stdio_command_does_not_enable_http_api_by_default()
        {
            var result = _parser.Parse("stdio");
            var binder = new ModelBinder<StartupOptions>();
            var options = (StartupOptions)binder.CreateInstance(new BindingContext(result));
            options.EnableHttpApi.Should().BeFalse();
        }

        [Fact]
        public void stdio_command_honors_default_kernel_option()
        {
            var result = _parser.Parse("stdio --default-kernel bsharp");
            var binder = new ModelBinder<KernelServerOptions>();
            var options = (KernelServerOptions)binder.CreateInstance(new BindingContext(result));
            options.DefaultKernel.Should().Be("bsharp");
        }

        [Fact]
        public async Task jupyter_command_returns_error_if_connection_file_path_is_not_passed()
        {
            var testConsole = new TestConsole();
            await _parser.InvokeAsync("jupyter", testConsole);

            testConsole.Error.ToString().Should().Contain("Required argument missing for command: jupyter");
        }
    }
}
