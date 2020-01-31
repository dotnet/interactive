// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine.Binding;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Server;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.DotNet.Interactive.Utility;
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
        public void kernel_server_starts_with_default_kernel()
        {
            var result = _parser.Parse($"kernel-server");
            var binder = new ModelBinder<KernelServerOptions>();
            var options = (KernelServerOptions)binder.CreateInstance(new BindingContext(result));
            options.DefaultKernel.Should().Be("csharp");
        }

        [Fact]
        public void kernel_server__honors_default_kernel_option()
        {
            var result = _parser.Parse($"kernel-server --default-kernel bsharp");
            var binder = new ModelBinder<KernelServerOptions>();
            var options = (KernelServerOptions)binder.CreateInstance(new BindingContext(result));
            options.DefaultKernel.Should().Be("bsharp");
        }

        [Fact]
        public async Task jupyter_returns_error_if_connection_file_path_is_not_passed()
        {
            var testConsole = new TestConsole();
            await _parser.InvokeAsync("jupyter", testConsole);

            testConsole.Error.ToString().Should().Contain("Required argument missing for command: jupyter");
        }

        [Fact]
        public async Task kernel_server_honors_log_path()
        {
            using var logPath = DisposableDirectory.Create();
            using var outputReceived = new ManualResetEvent(false);
            var errorLines = new List<string>();

            // start as external process
            var dotnet = new Dotnet(logPath.Directory);
            using var kernelServerProcess = dotnet.StartProcess(
                args: $@"""{typeof(Program).Assembly.Location}"" kernel-server --log-path ""{logPath.Directory.FullName}""",
                output: _line => { outputReceived.Set(); },
                error: errorLines.Add);

            // wait for log file to be created
            var logFile = await logPath.Directory.WaitForFile(
                timeout: TimeSpan.FromSeconds(2),
                predicate: _file => true); // any matching file is the one we want
            errorLines.Should().BeEmpty();
            logFile.Should().NotBeNull("unable to find created log file");

            // submit code
            var submission = new SubmitCode("1+1");
            var submissionJson = KernelCommandEnvelope.Serialize(KernelCommandEnvelope.Create(submission));
            await kernelServerProcess.StandardInput.WriteLineAsync(submissionJson);
            await kernelServerProcess.StandardInput.FlushAsync();

            // wait for output to proceed
            var gotOutput = outputReceived.WaitOne(timeout: TimeSpan.FromSeconds(2));
            gotOutput.Should().BeTrue("expected to receive on stdout");

            // kill
            kernelServerProcess.StandardInput.Close(); // simulate Ctrl+C
            await Task.Delay(TimeSpan.FromSeconds(2)); // allow logs to be flushed
            kernelServerProcess.Kill();
            kernelServerProcess.WaitForExit(2000).Should().BeTrue();
            errorLines.Should().BeEmpty();

            // check log file for expected contents
            (await logFile.WaitForFileCondition(
                timeout: TimeSpan.FromSeconds(2),
                predicate: file => file.Length > 0))
                .Should().BeTrue("expected non-empty log file");
            var logFileContents = File.ReadAllText(logFile.FullName);
            logFileContents.Should().Contain("ℹ OnAssemblyLoad: ");
        }
    }
}
