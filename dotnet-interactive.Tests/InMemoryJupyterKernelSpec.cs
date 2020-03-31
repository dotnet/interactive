// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FSharp.Compiler.SourceCodeServices;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class InMemoryJupyterKernelSpec : IJupyterKernelSpec
    {
        private readonly bool _shouldInstallSucceed;
        private readonly bool _shouldUninstallSucceed;
        private readonly IReadOnlyCollection<string> _error;
        private List<string> _kernelSpecs = new List<string>();

        public InMemoryJupyterKernelSpec(
            bool shouldInstallSucceed,
            IReadOnlyCollection<string> error,
            bool shouldUninstallSucceed = true)
        {
            _shouldInstallSucceed = shouldInstallSucceed;
            _shouldUninstallSucceed = shouldUninstallSucceed;
            _error = error;
        }

        public List<string> InstalledKernelSpecs
        {
            get => _kernelSpecs;
        }

        public Task<CommandLineResult> InstallKernel(DirectoryInfo directory)
        {
            if (_shouldInstallSucceed)
            {
                var installPath = Path.Combine(Directory.GetCurrentDirectory(), directory.Name.ToLower());
                foreach (var kernelSpec in directory.GetFiles("kernel.json") )
                {
                    InstalledKernelSpecs.Add(File.ReadAllText(kernelSpec.FullName));
                }
                return Task.FromResult(
                    new CommandLineResult(
                        0,
                        output: new List<string> { $"[InstallKernelSpec] Installed kernelspec {directory.Name} in {installPath}" }));
            }

            return Task.FromResult(new CommandLineResult(1, error: _error));
        }

        public Task<CommandLineResult> UninstallKernel(DirectoryInfo directory)
        {
            if (_shouldUninstallSucceed)
            {
                return Task.FromResult(new CommandLineResult(0));
            }
            else
            {
                return Task.FromResult(new CommandLineResult(1));
            }
        }
    }
}