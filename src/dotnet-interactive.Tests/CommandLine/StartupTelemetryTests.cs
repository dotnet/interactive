// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Pocket;
using Xunit;
using CommandLineParser = Microsoft.DotNet.Interactive.App.CommandLine.CommandLineParser;

namespace Microsoft.DotNet.Interactive.App.Tests.CommandLine;

public class StartupTelemetryTests : IDisposable
{
    private readonly FakeTelemetrySender _fakeTelemetrySender;
    private readonly RootCommand _rootCommand;
    private readonly FileInfo _connectionFile;
    private readonly CompositeDisposable _disposables = new();
    private readonly FakeFirstTimeUseNoticeSentinel _firstTimeUseNoticeSentinel;

    public StartupTelemetryTests()
    {
        _connectionFile = new FileInfo(Path.GetTempFileName());

        Environment.SetEnvironmentVariable(TelemetrySender.TelemetryOptOutEnvironmentVariableName, "");

        _disposables.Add(() =>
        {
            _connectionFile.Delete();
            Environment.SetEnvironmentVariable(TelemetrySender.TelemetryOptOutEnvironmentVariableName, null);
        });

        _firstTimeUseNoticeSentinel = new FakeFirstTimeUseNoticeSentinel
        {
            SentinelExists = true
        };

        _fakeTelemetrySender = new FakeTelemetrySender(_firstTimeUseNoticeSentinel);

        _rootCommand = CommandLineParser.Create(
            new ServiceCollection(),
            startWebServer: _ => { },
            startJupyter: (_, _) => Task.FromResult(1),
            startStdio: (_, _) => Task.FromResult(1),
            telemetrySender: _fakeTelemetrySender);
    }

    public void Dispose() => _disposables.Dispose();

    [Fact]
    public async Task Jupyter_standalone_command_sends_telemetry()
    {
        var parseResult = _rootCommand.Parse($"jupyter {_connectionFile}");
        await parseResult.InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().Contain(
            x => x.EventName == "command" &&
                 x.Properties["verb"] == "JUPYTER".ToSha256Hash() &&
                 x.Properties["default-kernel"] == "CSHARP".ToSha256Hash());
    }

    [Fact]
    public async Task Jupyter_standalone_command_has_one_entry()
    {
        await _rootCommand.Parse($"jupyter {_connectionFile}").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task Jupyter_default_kernel_csharp_sends_telemetry()
    {
        await _rootCommand.Parse($"jupyter --default-kernel csharp {_connectionFile}").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().Contain(
            x => x.EventName == "command" &&
                 x.Properties["verb"] == "JUPYTER".ToSha256Hash() &&
                 x.Properties["default-kernel"] == "CSHARP".ToSha256Hash());
    }

    [Fact]
    public async Task Jupyter_default_kernel_csharp_has_one_entry()
    {
        await _rootCommand.Parse($"jupyter --default-kernel csharp {_connectionFile}").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task Jupyter_default_kernel_fsharp_sends_telemetry()
    {
        await _rootCommand.Parse($"jupyter --default-kernel fsharp {_connectionFile}").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().Contain(
            x => x.EventName == "command" &&
                 x.Properties["verb"] == "JUPYTER".ToSha256Hash() &&
                 x.Properties["default-kernel"] == "FSHARP".ToSha256Hash());
    }

    [Fact]
    public async Task Jupyter_default_kernel_fsharp_has_one_entry()
    {
        await _rootCommand.Parse($"jupyter --default-kernel fsharp {_connectionFile}").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task Jupyter_install_sends_telemetry()
    {
        await _rootCommand.Parse("jupyter install").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().Contain(
            x => x.EventName == "command" &&
                 x.Properties["verb"] == "JUPYTER".ToSha256Hash() &&
                 x.Properties["subcommand"] == "INSTALL".ToSha256Hash());
    }

    [Fact]
    public async Task Jupyter_install_has_one_entry()
    {
        await _rootCommand.Parse($"jupyter install").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task Jupyter_default_kernel_csharp_ignore_connection_file_sends_telemetry()
    {
        var tmp = Path.GetTempFileName();
        await _rootCommand.Parse($"jupyter --default-kernel csharp {_connectionFile}").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().Contain(
            x => x.EventName == "command" &&
                 x.Properties["verb"] == "JUPYTER".ToSha256Hash() &&
                 x.Properties["default-kernel"] == "CSHARP".ToSha256Hash());

    }

    [Fact]
    public async Task Jupyter_default_kernel_csharp_ignore_connection_file_has_one_entry()
    {
        await _rootCommand.Parse($"jupyter --default-kernel csharp {_connectionFile}").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task Jupyter_ignore_connection_file_sends_telemetry()
    {
        // Do not capture connection file
        await _rootCommand.Parse($"jupyter  {_connectionFile}").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().Contain(
            x => x.EventName == "command" &&
                 x.Properties["verb"] == "JUPYTER".ToSha256Hash() &&
                 x.Properties["default-kernel"] == "CSHARP".ToSha256Hash());
    }

    [Fact]
    public async Task Jupyter_ignore_connection_file_has_one_entry()
    {
        await _rootCommand.Parse($"jupyter  {_connectionFile}").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task Jupyter_with_verbose_option_sends_telemetry_just_for_jupyter_command()
    {
        await _rootCommand.Parse($"--verbose jupyter  {_connectionFile}").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().Contain(
            x => x.EventName == "command" &&
                 x.Properties["verb"] == "JUPYTER".ToSha256Hash() &&
                 x.Properties["default-kernel"] == "CSHARP".ToSha256Hash());
    }

    [Fact]
    public async Task Jupyter_with_verbose_option_has_one_entry()
    {
        await _rootCommand.Parse($"--verbose jupyter {_connectionFile}").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task Jupyter_with_invalid_argument_does_not_send_any_telemetry()
    {
        await _rootCommand.Parse("jupyter invalidargument").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Jupyter_default_kernel_with_invalid_kernel_does_not_send_any_telemetry()
    {
        // Do not capture anything, especially "oops".
        await _rootCommand.Parse($"jupyter --default-kernel oops {_connectionFile}").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Jupyter_command_sends_frontend_telemetry()
    {
        await _rootCommand.Parse($"jupyter {_connectionFile}").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().Contain(
            x => x.EventName == "command" &&
                 x.Properties.Count == 3 &&
                 x.Properties["verb"] == "JUPYTER".ToSha256Hash() &&
                 x.Properties["frontend"] == "jupyter" &&
                 x.Properties["default-kernel"] == "CSHARP".ToSha256Hash());
    }

    [Fact]
    public async Task Jupyter_install_command_sends_default_frontend_telemetry()
    {
        var defaultFrontend = GetDefaultFrontendName();
        await _rootCommand.Parse("jupyter install").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().Contain(
            x => x.EventName == "command" &&
                 x.Properties.Count == 3 &&
                 x.Properties["verb"] == "JUPYTER".ToSha256Hash() &&
                 x.Properties["frontend"] == defaultFrontend &&
                 x.Properties["subcommand"] == "INSTALL".ToSha256Hash());

    }

    [Fact]
    public async Task Invalid_command_is_does_not_send_any_telemetry()
    {
        await _rootCommand.Parse("invalidcommand").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task stdio_command_sends_frontend_telemetry()
    {
        _rootCommand.Parse("[synapse] stdio").Errors.Should().HaveCount(0);
        await _rootCommand.Parse("[synapse] stdio").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().Contain(
            x => x.EventName == "command" &&
                 x.Properties.Count == 3 &&
                 x.Properties["verb"] == "STDIO".ToSha256Hash() &&
                 x.Properties["frontend"] == "synapse" &&
                 x.Properties["default-kernel"] == "CSHARP".ToSha256Hash());
    }

    [Fact]
    public async Task githubCodeSpaces_is_a_valid_frontend_for_stdio()
    {
        Environment.SetEnvironmentVariable("CODESPACES", "true");
        try
        {
            await _rootCommand.Parse("[vscode] stdio").InvokeAsync(new() { Output = new StringWriter() });


            _fakeTelemetrySender.TelemetryEvents.Should().Contain(
                x => x.EventName == "command" &&
                     x.Properties.Count == 3 &&
                     x.Properties["verb"] == "STDIO".ToSha256Hash() &&
                     x.Properties["frontend"] == "gitHubCodeSpaces" &&
                     x.Properties["default-kernel"] == "CSHARP".ToSha256Hash());
        }
        finally
        {
            Environment.SetEnvironmentVariable("CODESPACES", null);
        }
    }

    [Fact]
    public async Task frontend_can_be_set_via_environment_variable()
    {
        Environment.SetEnvironmentVariable("DOTNET_INTERACTIVE_FRONTEND_NAME", "test_runner");
        try
        {
            await _rootCommand.Parse("stdio").InvokeAsync(new() { Output = new StringWriter() });


            _fakeTelemetrySender.TelemetryEvents.Should().Contain(
                x => x.EventName == "command" &&
                     x.Properties.Count == 3 &&
                     x.Properties["verb"] == "STDIO".ToSha256Hash() &&
                     x.Properties["frontend"] == "test_runner" &&
                     x.Properties["default-kernel"] == "CSHARP".ToSha256Hash());
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_INTERACTIVE_FRONTEND_NAME", null);
        }
    }

    [Fact]
    public async Task vscode_is_a_valid_frontend_for_stdio()
    {
        await _rootCommand.Parse("[vscode] stdio").InvokeAsync(new() { Output = new StringWriter() });

        _fakeTelemetrySender.TelemetryEvents.Should().Contain(
            x => x.EventName == "command" &&
                 x.Properties.Count == 3 &&
                 x.Properties["verb"] == "STDIO".ToSha256Hash() &&
                 x.Properties["frontend"] == "vscode" &&
                 x.Properties["default-kernel"] == "CSHARP".ToSha256Hash());
    }

    [Fact]
    public async Task stdio_command_sends_default_frontend_telemetry()
    {
        var defaultFrontend = GetDefaultFrontendName();
        await _rootCommand.Parse("stdio").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().Contain(
            x => x.EventName == "command" &&
                 x.Properties.Count == 3 &&
                 x.Properties["verb"] == "STDIO".ToSha256Hash() &&
                 x.Properties["frontend"] == defaultFrontend &&
                 x.Properties["default-kernel"] == "CSHARP".ToSha256Hash());
    }

    private static string GetDefaultFrontendName()
    {
        var frontendName = Environment.GetEnvironmentVariable("DOTNET_INTERACTIVE_FRONTEND_NAME");
        frontendName = string.IsNullOrWhiteSpace(frontendName) ? "unknown" : frontendName;
        return frontendName;
    }

    [Fact]
    public async Task stdio_standalone_command_sends_telemetry()
    {
        await _rootCommand.Parse("stdio").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().Contain(
            x => x.EventName == "command" &&
                 x.Properties.Count >= 2 &&
                 x.Properties["verb"] == "STDIO".ToSha256Hash() &&
                 x.Properties["default-kernel"] == "CSHARP".ToSha256Hash());
    }

    [Fact]
    public async Task stdio_command_has_one_entry()
    {
        await _rootCommand.Parse("stdio").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task stdio_default_kernel_csharp_sends_telemetry()
    {
        await _rootCommand.Parse("stdio --default-kernel csharp").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().Contain(
            x => x.EventName == "command" &&
                 x.Properties.Count >= 2 &&
                 x.Properties["verb"] == "STDIO".ToSha256Hash() &&
                 x.Properties["default-kernel"] == "CSHARP".ToSha256Hash());
    }

    [Fact]
    public async Task stdio_default_kernel_csharp_has_one_entry()
    {
        await _rootCommand.Parse("stdio --default-kernel csharp").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task stdio_default_kernel_fsharp_sends_telemetry()
    {
        await _rootCommand.Parse("stdio --default-kernel fsharp").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().Contain(
            x => x.EventName == "command" &&
                 x.Properties.Count >= 2 &&
                 x.Properties["verb"] == "STDIO".ToSha256Hash() &&
                 x.Properties["default-kernel"] == "FSHARP".ToSha256Hash());
    }

    [Fact]
    public async Task stdio_default_kernel_fsharp_has_one_entry()
    {
        await _rootCommand.Parse("stdio --default-kernel fsharp").InvokeAsync(new() { Output = new StringWriter() });
        _fakeTelemetrySender.TelemetryEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task Show_first_time_message_if_environment_variable_is_not_set_and_sentinel_does_not_exist()
    {
        var environmentVariableName = FirstTimeUseNoticeSentinel.SkipFirstTimeExperienceEnvironmentVariableName;
        var currentState = Environment.GetEnvironmentVariable(environmentVariableName);
        _firstTimeUseNoticeSentinel.SentinelExists = false;
        Environment.SetEnvironmentVariable(environmentVariableName, null);
        try
        {
            var output = new StringWriter();
            await _rootCommand.Parse($"jupyter  {_connectionFile}").InvokeAsync(new() { Output = output });
            output.ToString().Should().Contain(TelemetrySender.WelcomeMessage);
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
            var output = new StringWriter();
            await _rootCommand.Parse($"jupyter {_connectionFile}").InvokeAsync(new() { Output = output });
            output.ToString().Should().NotContain(TelemetrySender.WelcomeMessage);
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariableName, currentState);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task stdio_command_sends_frontend_telemetry_when_frontend_is_VS(
        bool isSkipFirstTimeExperienceEnvironmentVariableSet, bool firstTimeExperienceSentinelExists)
    {
        var environmentVariableName = FirstTimeUseNoticeSentinel.SkipFirstTimeExperienceEnvironmentVariableName;
        var currentState = Environment.GetEnvironmentVariable(environmentVariableName);
        if (isSkipFirstTimeExperienceEnvironmentVariableSet)
        {
            Environment.SetEnvironmentVariable(environmentVariableName, "1");
        }
        else
        {
            Environment.SetEnvironmentVariable(environmentVariableName, null);
        }

        try
        {
            var telemetrySender =
                new FakeTelemetrySender(
                    new FakeFirstTimeUseNoticeSentinel
                    {
                        SentinelExists = firstTimeExperienceSentinelExists
                    });

            var parser = CommandLineParser.Create(
                new ServiceCollection(),
                startWebServer: _ => { },
                startJupyter: (_, _) => Task.FromResult(1),
                startStdio: (_, _) => Task.FromResult(1),
                telemetrySender: telemetrySender);

            await parser.Parse(
                $"""
                 [vs] stdio --working-dir {Directory.GetCurrentDirectory()} --kernel-host 9628-5c7e913f-8966-4afe-8d37-cc863292a352
                 """).InvokeAsync(new() { Output = new StringWriter() });

            if (isSkipFirstTimeExperienceEnvironmentVariableSet || firstTimeExperienceSentinelExists)
            {
                telemetrySender.TelemetryEvents.Should().Contain(
                    x => x.EventName == "command" &&
                         x.Properties.Count == 3 &&
                         x.Properties["verb"] == "STDIO".ToSha256Hash() &&
                         x.Properties["frontend"] == "vs" &&
                         x.Properties["default-kernel"] == "CSHARP".ToSha256Hash());
            }
            else
            {
                telemetrySender.TelemetryEvents.Should().BeEmpty();
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable(environmentVariableName, currentState);
        }
    }
}