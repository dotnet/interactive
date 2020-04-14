// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    internal class InMemoryJupyterKernelSpecInstaller : IJupyterKernelSpecInstaller
    {
        private readonly bool _shouldInstallSucceed;
        private readonly bool _shouldUninstallSucceed;
        private readonly string _message;
        private readonly IConsole _console;

        public InMemoryJupyterKernelSpecInstaller(
            bool shouldInstallSucceed,
            string message,
            IConsole console,
            bool shouldUninstallSucceed = true)
        {
            _shouldInstallSucceed = shouldInstallSucceed;
            _shouldUninstallSucceed = shouldUninstallSucceed;
            _message = message;
            _console = console;
        }

        public List<string> InstalledKernelSpecs { get; } = new List<string>();

        public Task<bool> InstallKernel(DirectoryInfo kernelSpecPath, DirectoryInfo destination = null)
        {
            if (_shouldInstallSucceed)
            {
                var installPath = Path.Combine(Directory.GetCurrentDirectory(), kernelSpecPath.Name.ToLower());
                foreach (var kernelSpec in kernelSpecPath.GetFiles("kernel.json") )
                {
                    InstalledKernelSpecs.Add(File.ReadAllText(kernelSpec.FullName));
                }
                _console.Out.WriteLine(string.Join('\n', _message,
                    $"[InstallKernelSpec] Installed kernelspec {kernelSpecPath.Name} in {installPath}"));
                return Task.FromResult(true);
            }

            _console.Error.WriteLine(_message);
            return Task.FromResult(false);
        }

        public Task<bool> UninstallKernel(DirectoryInfo directory)
        {
            return Task.FromResult(_shouldUninstallSucceed);
        }
    }
}