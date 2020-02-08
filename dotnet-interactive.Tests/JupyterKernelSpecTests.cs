// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Xunit.Abstractions;
using static Microsoft.DotNet.Interactive.Tests.DirectoryUtility;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public abstract class JupyterKernelSpecTests : IDisposable
    {
        private readonly List<DirectoryInfo> _kernelInstallations = new List<DirectoryInfo>();
        private readonly ITestOutputHelper _output;

        protected JupyterKernelSpecTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public abstract IJupyterKernelSpec GetJupyterKernelSpec(bool success, IReadOnlyCollection<string> error = null);

        [FactDependsOnJupyterOnPath(Skip = "causing test run to abort so skipping while we investigate")]
        public async Task Returns_success_output_when_kernel_installation_succeeded()
        {
            //For the FileSystemJupyterKernelSpec, this fact needs jupyter to be on the path
            //To run this test for FileSystemJupyterKernelSpec open Visual Studio inside anaconda prompt or in a terminal with
            //path containing the environment variables for jupyter

            var kernelSpec = GetJupyterKernelSpec(true);
            var kernelDir = CreateDirectory();

            var result = await kernelSpec.InstallKernel(kernelDir);
            result.Error.Should().BeEmpty();
            result.ExitCode.Should().Be(0);

            _kernelInstallations.Add(new DirectoryInfo(kernelDir.Name));

            //The actual jupyter instance is returning the output in the error field
            result.Output.First().Should().MatchEquivalentOf($"[InstallKernelSpec] Installed kernelspec {kernelDir.Name} in *{kernelDir.Name}");
        }

        [FactDependsOnJupyterNotOnPath]
        public async Task Returns_failure_when_kernel_installation_did_not_succeed()
        {
            var kernelSpec = GetJupyterKernelSpec(false, error: new[] { "Could not find jupyter kernelspec module" });
            var kernelDir = CreateDirectory();

            var result = await kernelSpec.InstallKernel(kernelDir);
            result.ExitCode.Should().Be(1);
            result.Error.Should().BeEquivalentTo("Could not find jupyter kernelspec module");
        }

        public void Dispose()
        {
            var kernelSpec = GetJupyterKernelSpec(true);

            foreach (var directory in _kernelInstallations)
            {
                Task.Run(() =>
                {
                    try
                    {
                        kernelSpec.UninstallKernel(directory);
                    }
                    catch (Exception exception)
                    {
                        _output.WriteLine($"Exception swallowed while disposing {GetType()}: {exception}");
                    }
                }).Wait();
            }
        }
    }

    public class JupyterKernelSpecIntegrationTests : JupyterKernelSpecTests
    {
        public JupyterKernelSpecIntegrationTests(ITestOutputHelper output) : base(output)
        {
        }

        public override IJupyterKernelSpec GetJupyterKernelSpec(bool success, IReadOnlyCollection<string> error = null)
        {
            return new JupyterKernelSpec();
        }
    }

    public class InMemoryJupyterKernelSpecTests : JupyterKernelSpecTests
    {
        public InMemoryJupyterKernelSpecTests(ITestOutputHelper output) : base(output)
        {
        }

        public override IJupyterKernelSpec GetJupyterKernelSpec(bool success, IReadOnlyCollection<string> error = null)
        {
            return new InMemoryJupyterKernelSpec(success, error);
        }
    }
}