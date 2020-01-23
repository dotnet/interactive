// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Pocket;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests.CommandLine
{
    public class TelemetryTests : IDisposable
    {
        private readonly FakeTelemetry _fakeTelemetry;
        private readonly TestConsole _console = new TestConsole();
        private readonly Parser _parser;
        private readonly FileInfo _connectionFile;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public TelemetryTests()
        {
            _fakeTelemetry = new FakeTelemetry();

            _connectionFile = new FileInfo(Path.GetTempFileName());

            _disposables.Add(() => _connectionFile.Delete());

            _parser = CommandLineParser.Create(
                new ServiceCollection(),
                startServer: (options, invocationContext) => { },
                jupyter: (startupOptions, console, startServer, context) => Task.FromResult(1),
                startKernelServer: (startupOptions, kernel, console) => Task.FromResult(1),
                telemetry: _fakeTelemetry,
                firstTimeUseNoticeSentinel: new NopFirstTimeUseNoticeSentinel());
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        [Fact]
        public async Task Jupyter_standalone_command_sends_telemetry()
        {
            await _parser.InvokeAsync($"jupyter {_connectionFile}", _console);
            _fakeTelemetry.LogEntries.Should().Contain(
                x => x.EventName == "command" &&
                     x.Properties.Count == 2 &&
                     x.Properties["verb"] == Sha256Hasher.Hash("JUPYTER") &&
                     x.Properties["default-kernel"] == Sha256Hasher.Hash("CSHARP"));
        }

        [Fact]
        public async Task Jupyter_standalone_command_has_one_entry()
        {
            await _parser.InvokeAsync($"jupyter {_connectionFile}", _console);
            _fakeTelemetry.LogEntries.Should().HaveCount(1);
        }

        [Fact]
        public async Task Jupyter_default_kernel_csharp_sends_telemetry()
        {
            await _parser.InvokeAsync($"jupyter --default-kernel csharp {_connectionFile}", _console);
            _fakeTelemetry.LogEntries.Should().Contain(
                x => x.EventName == "command" &&
                     x.Properties.Count == 2 &&
                     x.Properties["verb"] == Sha256Hasher.Hash("JUPYTER") &&
                     x.Properties["default-kernel"] == Sha256Hasher.Hash("CSHARP"));
        }

        [Fact]
        public async Task Jupyter_default_kernel_csharp_has_one_entry()
        {
            await _parser.InvokeAsync($"jupyter --default-kernel csharp {_connectionFile}", _console);
            _fakeTelemetry.LogEntries.Should().HaveCount(1);
        }

        [Fact]
        public async Task Jupyter_default_kernel_fsharp_sends_telemetry()
        {
            await _parser.InvokeAsync($"jupyter --default-kernel fsharp {_connectionFile}", _console);
            _fakeTelemetry.LogEntries.Should().Contain(
                x => x.EventName == "command" &&
                     x.Properties.Count == 2 &&
                     x.Properties["verb"] == Sha256Hasher.Hash("JUPYTER") &&
                     x.Properties["default-kernel"] == Sha256Hasher.Hash("FSHARP"));
        }

        [Fact]
        public async Task Jupyter_default_kernel_fsharp_has_one_entry()
        {
            await _parser.InvokeAsync($"jupyter --default-kernel fsharp {_connectionFile}", _console);
            _fakeTelemetry.LogEntries.Should().HaveCount(1);
        }

        [Fact]
        public async Task Jupyter_install_sends_telemetry()
        {
            await _parser.InvokeAsync($"jupyter install", _console);
            _fakeTelemetry.LogEntries.Should().Contain(
                x => x.EventName == "command" &&
                     x.Properties.Count == 2 &&
                     x.Properties["verb"] == Sha256Hasher.Hash("JUPYTER") &&
                     x.Properties["subcommand"] == Sha256Hasher.Hash("INSTALL"));
        }

        [Fact]
        public async Task Jupyter_install_has_one_entry()
        {
            await _parser.InvokeAsync($"jupyter install", _console);
            _fakeTelemetry.LogEntries.Should().HaveCount(1);
        }

        [Fact]
        public async Task Jupyter_default_kernel_csharp_ignore_connection_file_sends_telemetry()
        {
            var tmp = Path.GetTempFileName();
            await _parser.InvokeAsync($"jupyter --default-kernel csharp {_connectionFile}", _console);
            _fakeTelemetry.LogEntries.Should().Contain(
                x => x.EventName == "command" &&
                     x.Properties.Count == 2 &&
                     x.Properties["verb"] == Sha256Hasher.Hash("JUPYTER") &&
                     x.Properties["default-kernel"] == Sha256Hasher.Hash("CSHARP"));

        }

        [Fact]
        public async Task Jupyter_default_kernel_csharp_ignore_connection_file_has_one_entry()
        {
            await _parser.InvokeAsync($"jupyter --default-kernel csharp {_connectionFile}", _console);
            _fakeTelemetry.LogEntries.Should().HaveCount(1);
        }

        [Fact]
        public async Task Jupyter_ignore_connection_file_sends_telemetry()
        {
                // Do not capture connection file
                await _parser.InvokeAsync($"jupyter  {_connectionFile}", _console);
                _fakeTelemetry.LogEntries.Should().Contain(
                    x => x.EventName == "command" &&
                         x.Properties.Count == 2 &&
                         x.Properties["verb"] == Sha256Hasher.Hash("JUPYTER") &&
                         x.Properties["default-kernel"] == Sha256Hasher.Hash("CSHARP"));
        }

        [Fact]
        public async Task Jupyter_ignore_connection_file_has_one_entry()
        {
                await _parser.InvokeAsync($"jupyter  {_connectionFile}", _console);
                _fakeTelemetry.LogEntries.Should().HaveCount(1);
        }

        [Fact]
        public async Task Jupyter_with_verbose_option_sends_telemetry_just_for_juptyer_command()
        {
            await _parser.InvokeAsync($"--verbose jupyter  {_connectionFile}", _console);
            _fakeTelemetry.LogEntries.Should().Contain(
                x => x.EventName == "command" &&
                     x.Properties.Count == 2 &&
                     x.Properties["verb"] == Sha256Hasher.Hash("JUPYTER") &&
                     x.Properties["default-kernel"] == Sha256Hasher.Hash("CSHARP"));
        }

        [Fact]
        public async Task Jupyter_with_verbose_option_has_one_entry()
        {
            await _parser.InvokeAsync($"--verbose jupyter {_connectionFile}", _console);
            _fakeTelemetry.LogEntries.Should().HaveCount(1);
        }

        [Fact]
        public async Task Jupyter_with_invalid_argument_does_not_send_any_telemetry()
        {
            await _parser.InvokeAsync($"jupyter invalidargument", _console);
            _fakeTelemetry.LogEntries.Should().BeEmpty();
        }

        [Fact]
        public async Task Jupyter_default_kernel_with_invalid_kernel_does_not_send_any_telemetry()
        {
            // Do not capture anything, especially "oops".
            await _parser.InvokeAsync($"jupyter --default-kernel oops {_connectionFile}", _console);
            _fakeTelemetry.LogEntries.Should().BeEmpty();
        }

        [Fact]
        public async Task Invalid_command_is_does_not_send_any_telemetry()
        {
            await _parser.InvokeAsync("invalidcommand", _console);
            _fakeTelemetry.LogEntries.Should().BeEmpty();
        }

        [Fact]
        public async Task Kernel_server_standalone_command_sends_telemetry()
        {
            await _parser.InvokeAsync("kernel-server", _console);
            _fakeTelemetry.LogEntries.Should().Contain(
                x => x.EventName == "command" &&
                     x.Properties.Count == 2 &&
                     x.Properties["verb"] == Sha256Hasher.Hash("KERNEL-SERVER") &&
                     x.Properties["default-kernel"] == Sha256Hasher.Hash("CSHARP"));
        }

        [Fact]
        public async Task Kernel_server_standalone_command_has_one_entry()
        {
            await _parser.InvokeAsync("kernel-server", _console);
            _fakeTelemetry.LogEntries.Should().HaveCount(1);
        }

        [Fact]
        public async Task Kernel_server_default_kernel_csharp_sends_telemetry()
        {
            await _parser.InvokeAsync("kernel-server --default-kernel csharp", _console);
            _fakeTelemetry.LogEntries.Should().Contain(
                x => x.EventName == "command" &&
                     x.Properties.Count == 2 &&
                     x.Properties["verb"] == Sha256Hasher.Hash("KERNEL-SERVER") &&
                     x.Properties["default-kernel"] == Sha256Hasher.Hash("CSHARP"));
        }

        [Fact]
        public async Task Kernel_server_default_kernel_csharp_has_one_entry()
        {
            await _parser.InvokeAsync("kernel-server --default-kernel csharp", _console);
            _fakeTelemetry.LogEntries.Should().HaveCount(1);
        }

        [Fact]
        public async Task Kernel_server_default_kernel_fsharp_sends_telemetry()
        {
            await _parser.InvokeAsync("kernel-server --default-kernel fsharp", _console);
            _fakeTelemetry.LogEntries.Should().Contain(
                x => x.EventName == "command" &&
                     x.Properties.Count == 2 &&
                     x.Properties["verb"] == Sha256Hasher.Hash("KERNEL-SERVER") &&
                     x.Properties["default-kernel"] == Sha256Hasher.Hash("FSHARP"));
        }

        [Fact]
        public async Task Kernel_server_default_kernel_fsharp_has_one_entry()
        {
            await _parser.InvokeAsync("kernel-server --default-kernel fsharp", _console);
            _fakeTelemetry.LogEntries.Should().HaveCount(1);
        }

        [Fact]
        public async Task Show_first_time_message_if_environment_variable_is_not_set()
        {
            var environmentVariableName = FirstTimeUseNoticeSentinel.SkipFirstTimeExperienceEnvironmentVariableName;
            var currentState = Environment.GetEnvironmentVariable(environmentVariableName);
            Environment.SetEnvironmentVariable(environmentVariableName, null);
            try
            {
                await _parser.InvokeAsync($"jupyter  {_connectionFile}", _console);
                _console.Out.ToString().Should().Contain(Telemetry.Telemetry.WelcomeMessage);
            }
            finally
            {
                Environment.SetEnvironmentVariable(environmentVariableName, currentState);
            }
        }

        [Fact]
        public async Task Do_not_show_first_time_message_if_environment_variable_is_set()
        {
            var environmentVariableName = FirstTimeUseNoticeSentinel.SkipFirstTimeExperienceEnvironmentVariableName;
            var currentState = Environment.GetEnvironmentVariable(environmentVariableName);
            Environment.SetEnvironmentVariable(environmentVariableName, null);
            Environment.SetEnvironmentVariable(environmentVariableName, "1");
            try
            {
                await _parser.InvokeAsync($"jupyter {_connectionFile}", _console);
                _console.Out.ToString().Should().NotContain(Telemetry.Telemetry.WelcomeMessage);
            }
            finally
            {
                Environment.SetEnvironmentVariable(environmentVariableName, currentState);
            }
        }
    }
}
