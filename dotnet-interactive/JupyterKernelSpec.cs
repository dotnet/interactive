// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.App
{
    public class JupyterKernelSpec : IJupyterKernelSpec
    {
        private readonly PortRange _httpPortRange;

        public JupyterKernelSpec(PortRange httpPortRange = null)
        {
            _httpPortRange = httpPortRange;
        }

        public async Task<CommandLineResult> ExecuteCommand(string command, string args = "")
        {
            if (!JupyterKernelSpecExists())
            {
                return new CommandLineResult(1, error: new List<string>
                {
                    "Could not find jupyter kernelspec module"
                });
            }

            return await Utility.CommandLine.Execute("jupyter", $"kernelspec {command} {args}");
        }

        public Task<CommandLineResult> InstallKernel(DirectoryInfo sourceDirectory)
        {
            return ExecuteCommand($@"install ""{sourceDirectory.FullName}""", "--user");
        }
        
        public Task<CommandLineResult> UninstallKernel(DirectoryInfo sourceDirectory)
        {
            return ExecuteCommand($@"uninstall ""{sourceDirectory.FullName}""");
        }

        public static bool JupyterKernelSpecExists()
        {
            var command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                              ? "where" 
                              : "which";

            var jupyterKernelSpecExists = false;

            Task.Run(async ()=> {
                var result = await Utility.CommandLine.Execute(command, "jupyter-kernelspec");
                jupyterKernelSpecExists = result.ExitCode == 0;
            }).Wait(2000);

            return jupyterKernelSpecExists;
        }
    }
}
