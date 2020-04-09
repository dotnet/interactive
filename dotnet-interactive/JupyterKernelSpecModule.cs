// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.App
{
    internal class JupyterKernelSpecModule 
    {

        public async Task<CommandLineResult> ExecuteCommand(string command, string args = "")
        {
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

    }
}