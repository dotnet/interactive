// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    internal class InMemoryJupyterKernelSpecInstaller : IJupyterKernelSpecInstaller
    {
        private readonly bool _shouldInstallSucceed;
        private readonly bool _shouldUninstallSucceed;
        private readonly string _message;

        public InMemoryJupyterKernelSpecInstaller(
            bool shouldInstallSucceed,
            string message,
            bool shouldUninstallSucceed = true)
        {
            _shouldInstallSucceed = shouldInstallSucceed;
            _shouldUninstallSucceed = shouldUninstallSucceed;
            _message = message;
        }

        public List<string> InstalledKernelSpecs { get; } = new List<string>();

        public Task<KernelSpecInstallResult> InstallKernel(DirectoryInfo kernelSpecPath, DirectoryInfo destination = null)
        {
            if (_shouldInstallSucceed)
            {
                var installPath = Path.Combine(Directory.GetCurrentDirectory(), kernelSpecPath.Name.ToLower());
                foreach (var kernelSpec in kernelSpecPath.GetFiles("kernel.json") )
                {
                    InstalledKernelSpecs.Add(File.ReadAllText(kernelSpec.FullName));
                }
                return Task.FromResult(
                    new KernelSpecInstallResult(
                        true,
                        string.Join('\n', _message, 
                        $"[InstallKernelSpec] Installed kernelspec {kernelSpecPath.Name} in {installPath}" )));
            }

            return Task.FromResult(new KernelSpecInstallResult(false,  _message));
        }

        public Task<KernelSpecInstallResult> UninstallKernel(DirectoryInfo directory)
        {
            if (_shouldUninstallSucceed)
            {
                return Task.FromResult(new KernelSpecInstallResult(true));
            }
            else
            {
                return Task.FromResult(new KernelSpecInstallResult(false));
            }
        }
    }
}