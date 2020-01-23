// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class JupyterInstallCommandTests
    {
        [Fact]
        public async Task Returns_error_when_jupyter_paths_could_not_be_obtained()
        {
            var console = new TestConsole();
            var installCommand = new JupyterInstallCommand(
                console, 
                new InMemoryJupyterKernelSpec(false, error: new[] { "Could not find jupyter kernelspec module" }));
            
            await installCommand.InvokeAsync();
            
            console.Error.ToString().Should()
                   .Contain(".NET kernel installation failed")
                   .And
                   .Contain("Could not find jupyter kernelspec module");
        }

        [Fact]
        public async Task Prints_to_console_when_kernel_installation_succeeded()
        {
            var console = new TestConsole();
            var jupyterCommandLine = new JupyterInstallCommand(console, new InMemoryJupyterKernelSpec(true, null));

            await jupyterCommandLine.InvokeAsync();
            
            var consoleOut = console.Out.ToString();
            consoleOut.Should().MatchEquivalentOf("*[InstallKernelSpec] Installed kernelspec .net-csharp in *.net-csharp*");
            consoleOut.Should().MatchEquivalentOf("*[InstallKernelSpec] Installed kernelspec .net-fsharp in *.net-fsharp*");
            consoleOut.Should().Contain(".NET kernel installation succeeded");
        }
    }
}