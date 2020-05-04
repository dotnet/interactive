// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    internal class JupyterKernelSpecModuleSimulator : IJupyterKernelSpecModule
    {
        private readonly bool _success;
        private readonly DirectoryInfo _defaultKernelSpecDirectory;
        private readonly Exception _withException;

        public JupyterKernelSpecModuleSimulator(bool success, DirectoryInfo defaultKernelSpecDirectory= null, Exception withException = null)
        {
            _success = success;
            _defaultKernelSpecDirectory = defaultKernelSpecDirectory;
            _withException = withException;
        }

        public List<string> InstalledKernelSpecs { get; } = new List<string>();

        public Task<CommandLineResult> InstallKernel(DirectoryInfo sourceDirectory)
        {
            foreach (var kernelSpec in sourceDirectory.GetFiles("kernel.json"))
            {
                InstalledKernelSpecs.Add(File.ReadAllText(kernelSpec.FullName));
            }

            if (!_success && _withException != null)
            {
                throw _withException;
            }
            return Task.FromResult(new CommandLineResult(_success ? 0 : 1));
        }

        public DirectoryInfo GetDefaultKernelSpecDirectory()
        {
            return _success
                ? _defaultKernelSpecDirectory ?? new JupyterKernelSpecModule().GetDefaultKernelSpecDirectory()
                : _defaultKernelSpecDirectory?? new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetTempFileName()));
        }
    }
}