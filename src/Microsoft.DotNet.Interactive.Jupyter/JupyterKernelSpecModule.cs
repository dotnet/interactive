// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Utility;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class JupyterKernelSpecModule : IJupyterKernelSpecModule
    {
        private Dictionary<string, DirectoryInfo> _installedKernelDirs = null;

        private async Task<CommandLineResult> ExecuteCommand(string command, string args = "")
        {
            return await CommandLine.Execute("jupyter", $"kernelspec {command} {args}");
        }

        public Task<CommandLineResult> InstallKernel(DirectoryInfo sourceDirectory)
        {
            return ExecuteCommand($@"install ""{sourceDirectory.FullName}""", "--user");
        }

        public IReadOnlyDictionary<string, DirectoryInfo> GetInstalledKernelDirectories()
        {
            // do this only once. If a new kernel is installed, the kernel has to be reloaded. 
            if (_installedKernelDirs == null)
            {
                _installedKernelDirs = new Dictionary<string, DirectoryInfo>();

                var dataDirectories = JupyterCommonDirectories.GetDataDirectories();
                foreach (var directory in dataDirectories)
                {
                    var kernelDir = new DirectoryInfo(Path.Combine(directory.FullName, "kernels"));
                    if (kernelDir.Exists)
                    {
                        var kernels = kernelDir.GetDirectories();
                        foreach (var kernel in kernels)
                        {
                            if (!_installedKernelDirs.ContainsKey(kernel.Name))
                            {
                                _installedKernelDirs.Add(kernel.Name, kernel);
                            }
                        }
                    }
                }
            }

            return _installedKernelDirs;
        }

        public DirectoryInfo GetDefaultKernelSpecDirectory()
        {
            var dataDirectory = JupyterCommonDirectories.GetDataDirectory();
            var directory = new DirectoryInfo(Path.Combine(dataDirectory.FullName, "kernels"));
            return directory;
        }
    }
}