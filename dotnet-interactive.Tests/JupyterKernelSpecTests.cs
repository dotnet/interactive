// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit.Abstractions;
using static Microsoft.DotNet.Interactive.Tests.Utility.DirectoryUtility;

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

        public abstract IJupyterKernelSpecInstaller GetJupyterKernelSpec(bool success, string message = null);

        [FactDependsOnJupyterOnPath(Skip = "causing test run to abort so skipping while we investigate")]
        public async Task Returns_success_output_when_kernel_installation_succeeded()
        {
            //For the FileSystemJupyterKernelSpec, this fact needs jupyter to be on the path
            //To run this test for FileSystemJupyterKernelSpec open Visual Studio inside anaconda prompt or in a terminal with
            //path containing the environment variables for jupyter

            var kernelSpec = GetJupyterKernelSpec(true);
            var kernelDir = CreateDirectory();

            var result = await kernelSpec.InstallKernel(kernelDir);
            result.Succeeded.Should().BeTrue();

            _kernelInstallations.Add(new DirectoryInfo(kernelDir.Name));

            //The actual jupyter instance is returning the output in the error field
            result.Message.Should().MatchEquivalentOf($"[InstallKernelSpec] Installed kernelspec {kernelDir.Name} in *{kernelDir.Name}");
        }

        [FactDependsOnJupyterNotOnPath]
        public async Task Uses_default_paths_when_kernelspec_module_is_not_on_path()
        {
            var kernelSpec = GetJupyterKernelSpec(true, message:  "kernelspec module not available, Installing using default paths" );
            var kernelDir = CreateDirectory();

            var result = await kernelSpec.InstallKernel(kernelDir);
            result.Succeeded.Should().BeTrue();
            result.Message.Should().Match("kernelspec module not available, Installing using default paths*");
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

        public override IJupyterKernelSpecInstaller GetJupyterKernelSpec(bool success, string message = null)
        {
            return new JupyterKernelSpecInstaller();
        }
    }

    public class InMemoryJupyterKernelSpecTests : JupyterKernelSpecTests
    {
        public InMemoryJupyterKernelSpecTests(ITestOutputHelper output) : base(output)
        {
        }

        public override IJupyterKernelSpecInstaller GetJupyterKernelSpec(bool success, string message = null)
        {
            return new InMemoryJupyterKernelSpecInstaller(success, message);
        }
    }
}