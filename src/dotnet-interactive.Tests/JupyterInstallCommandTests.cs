// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class JupyterInstallCommandTests
    {
        [Fact]
        public async Task Appends_http_port_range_arguments()
        {
            var console = new TestConsole();
            var kernelSpecModule = new JupyterKernelSpecModuleSimulator(true);
            var kernelSpecInstaller = new JupyterKernelSpecInstaller(console,kernelSpecModule);
            var jupyterCommandLine = new JupyterInstallCommand(console, kernelSpecInstaller, new HttpPortRange(100, 400));

            await jupyterCommandLine.InvokeAsync();

            kernelSpecModule.InstalledKernelSpecs
                .Should()
                .HaveCount(3)
                .And
                .Match(s => s.All(k => k.Contains("--http-port-range")));

        }

        [Fact]
        public async Task Returns_error_when_jupyter_paths_could_not_be_obtained()
        {
            var console = new TestConsole();
            var kernelSpecModule = new JupyterKernelSpecModuleSimulator(false);
            var kernelSpecInstaller = new JupyterKernelSpecInstaller(console, kernelSpecModule);
            var installCommand = new JupyterInstallCommand(
                console,
                kernelSpecInstaller);

            await installCommand.InvokeAsync();
            var consoleError = console.Error.ToString();
            using var scope = new AssertionScope();
            consoleError.Should().Contain("Failed to install \".NET (F#)\" kernel.");
            consoleError.Should().Contain("Failed to install \".NET (C#)\" kernel.");
            consoleError.Should().Contain("Failed to install \".NET (PowerShell)\" kernel.");
        }

        [Fact]
        public async Task Prints_to_console_when_kernel_installation_succeeded()
        {
            var console = new TestConsole();
            var kernelSpecModule = new JupyterKernelSpecModuleSimulator(true);
            var kernelSpecInstaller = new JupyterKernelSpecInstaller(console, kernelSpecModule);
            var jupyterCommandLine = new JupyterInstallCommand(console, kernelSpecInstaller);

            await jupyterCommandLine.InvokeAsync();

            var consoleOut = console.Out.ToString();

            using var scope = new AssertionScope();

            consoleOut.Should().Contain("Installed \".NET (F#)\" kernel.");
            consoleOut.Should().Contain("Installed \".NET (C#)\" kernel.");
        }
    }
}