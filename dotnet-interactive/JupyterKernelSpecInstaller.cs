// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.App
{
    public class JupyterKernelSpecInstaller : IJupyterKernelSpecInstaller
    {
        private readonly JupyterKernelSpecModule _kernelSpecModule;

        public JupyterKernelSpecInstaller()
        {
            _kernelSpecModule = new JupyterKernelSpecModule();
        }
        public async Task<KernelSpecInstallResults> InstallKernel(DirectoryInfo sourceDirectory, DirectoryInfo destination = null)
        {

            if (destination != null)
            {
                var (succeeded, message) = CopyKernelSpecFiles(sourceDirectory, destination);
                return new KernelSpecInstallResults(succeeded, message);
            }
            else 
            {
                try
                {
                    var result = await _kernelSpecModule.InstallKernel(sourceDirectory);
                    if (result.ExitCode == 0)
                    {
                        return new KernelSpecInstallResults(true,
                            string.Join('\n', result.Output.Concat(result.Error)));
                    }
                }
                catch (Win32Exception w32e)
                {
                    // file not found when executing process
                    if (!w32e.Source.Contains(typeof(System.Diagnostics.Process).FullName))
                    {
                        throw;
                    }
                }

                var location = GetDefaultDirectory();
                var (succeeded, message) = CopyKernelSpecFiles(sourceDirectory, location);
                return new KernelSpecInstallResults(succeeded, string.Join('\n', "kernelspec module not available, Installing using default paths", message));
            }
        }
        
        public async Task<KernelSpecInstallResults> UninstallKernel(DirectoryInfo sourceDirectory)
        {
            var commandLineResult = await _kernelSpecModule.UninstallKernel(sourceDirectory);
            var message = string.Join('\n', commandLineResult.Output.Concat(commandLineResult.Error));

            var result = new KernelSpecInstallResults(commandLineResult.ExitCode == 0,
                message
            );

            return result;
        }


        private (bool succeeded, string message) CopyKernelSpecFiles(DirectoryInfo source, DirectoryInfo location)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (!location.Exists)
            {
                return (false,  $"Directory {location.FullName} does not exists");
            }

            string message;
            var success = true;
            var destination = new DirectoryInfo(Path.Combine(location.FullName, source.Name));

            try
            {
                if (destination.Exists)
                {
                    destination.Delete(true);
                   message = $"[InstallKernelSpec] Removing existing kernelspec in {destination.FullName}";
                }

                destination.Create();

                // First create all of the directories
                foreach (var dirPath in Directory.GetDirectories(source.FullName, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(source.FullName, destination.FullName));
                }

                // Copy all the files
                foreach (var newPath in Directory.GetFiles(source.FullName, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(source.FullName, destination.FullName));
                }


                message = ($"[InstallKernelSpec] Installed kernelspec {source.Name} in {destination.FullName}");

            }
            catch (IOException ioe)
            {
                success = false;
                message  = (ioe.Message);
            }

            return (success, message);
        }


        private DirectoryInfo GetDefaultDirectory()
        {
            DirectoryInfo directory;
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                    directory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "jupyter", "kernels"));
                    break;
                case PlatformID.Unix:
                    directory = new DirectoryInfo("~/.local/share/jupyter/kernels");
                    break;
                case PlatformID.MacOSX:
                    directory = new DirectoryInfo("~/Library/Jupyter/kernels");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return directory;
        }
    }
}
