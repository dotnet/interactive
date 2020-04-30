// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.App
{
    public class JupyterKernelSpecModule: IJupyterKernelSpecModule
    {
        private async Task<CommandLineResult> ExecuteCommand(string command, string args = "")
        {
            return await Utility.CommandLine.Execute("jupyter", $"kernelspec {command} {args}");
        }

        public Task<CommandLineResult> InstallKernel(DirectoryInfo sourceDirectory)
        {
            return ExecuteCommand($@"install ""{sourceDirectory.FullName}""", "--user");
        }

        public  DirectoryInfo GetDefaultKernelSpecDirectory()
        {
            
            var directory = GetDefaultAnacondaKernelSpecDirectory();
            if (!directory.Exists)
            {
                directory = GetDefaultJupyterKernelSpecDirectory();
            }

            return directory;
        }

        private static DirectoryInfo GetDefaultAnacondaKernelSpecDirectory()
        {
            DirectoryInfo directory;
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                    directory = new DirectoryInfo(Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Continuum", "anaconda3", "share", "jupyter", "kernels"));
                    break;
                case PlatformID.Unix:
                    directory = new DirectoryInfo("~/anaconda3/share/jupyter/kernels");
                    break;
                case PlatformID.MacOSX:
                    directory = new DirectoryInfo("~/opt/anaconda3/share/jupyter/kernels");
                    break;
                default:
                    throw new PlatformNotSupportedException();
            }

            return directory;
        }

        private static DirectoryInfo GetDefaultJupyterKernelSpecDirectory()
        {
            DirectoryInfo directory;
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                    directory = new DirectoryInfo(Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "jupyter", "kernels"));
                    break;
                case PlatformID.Unix:
                    directory = new DirectoryInfo("~/.local/share/jupyter/kernels");
                    break;
                case PlatformID.MacOSX:
                    directory = new DirectoryInfo("~/Library/Jupyter/kernels");
                    break;
                default:
                    throw new PlatformNotSupportedException();
            }

            return directory;
        }
    }
}