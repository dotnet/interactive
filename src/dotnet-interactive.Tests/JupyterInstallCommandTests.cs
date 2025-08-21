// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Assent;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Http;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests;

public class JupyterInstallCommandTests
{
    [Fact]
    public async Task Appends_http_port_range_arguments()
    {
        var kernelSpecModule = new JupyterKernelSpecModuleSimulator(true);
        var kernelSpecInstaller = new JupyterKernelSpecInstaller(new StringWriter(), new StringWriter(), kernelSpecModule);
        var jupyterCommandLine = new JupyterInstallCommand(kernelSpecInstaller, new HttpPortRange(100, 400));

        await jupyterCommandLine.InvokeAsync();

        kernelSpecModule.InstalledKernelSpecs
            .Should()
            .HaveCount(3)
            .And
            .Match(s => s.All(k => k.Contains("--http-port-range")));

    }

    [Theory]
    [InlineData(".NET (C#)")]
    [InlineData(".NET (F#)")]
    [InlineData(".NET (PowerShell)")]
    [Trait("Category", "Contracts and serialization")]
    public async Task kernel_spec_is_not_broken(string displayName)
    {
        var _configuration = new Configuration()
            .UsingExtension($"kernelspec_{displayName.Replace(".", "_").Replace(" ", "_")}.txt")
            .SetInteractive(Debugger.IsAttached);

        var kernelSpecModule = new JupyterKernelSpecModuleSimulator(true);
        var kernelSpecInstaller = new JupyterKernelSpecInstaller(new StringWriter(), new StringWriter(), kernelSpecModule);
        var jupyterCommandLine = new JupyterInstallCommand(kernelSpecInstaller, new HttpPortRange(100, 400));

        await jupyterCommandLine.InvokeAsync();

        var kernelSpec = kernelSpecModule.InstalledKernelSpecs.Single(k => k.Contains(displayName));

        this.Assent(kernelSpec, _configuration);
    }

    [Fact]
    public async Task Returns_error_when_jupyter_paths_could_not_be_obtained()
    {
        var kernelSpecModule = new JupyterKernelSpecModuleSimulator(false);
        var error = new StringWriter();
        var kernelSpecInstaller = new JupyterKernelSpecInstaller(new StringWriter(), error, kernelSpecModule);
        var installCommand = new JupyterInstallCommand(kernelSpecInstaller);

        await installCommand.InvokeAsync();
        var consoleError = error.ToString();
        using var scope = new AssertionScope();
        consoleError.Should().Contain("Failed to install \".NET (F#)\" kernel.");
        consoleError.Should().Contain("Failed to install \".NET (C#)\" kernel.");
        consoleError.Should().Contain("Failed to install \".NET (PowerShell)\" kernel.");
    }

    [Fact]
    public async Task Prints_to_console_when_kernel_installation_succeeded()
    {
        var kernelSpecModule = new JupyterKernelSpecModuleSimulator(true);
        var output = new StringWriter();
        var kernelSpecInstaller = new JupyterKernelSpecInstaller(output, new StringWriter(), kernelSpecModule);
        var jupyterCommandLine = new JupyterInstallCommand(kernelSpecInstaller);

        await jupyterCommandLine.InvokeAsync();

        var consoleOut = output.ToString();

        using var scope = new AssertionScope();

        consoleOut.Should().Contain("Installed \".NET (F#)\" kernel.");
        consoleOut.Should().Contain("Installed \".NET (C#)\" kernel.");
    }
}