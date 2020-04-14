// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.App
{
    public class JupyterKernelSpecInstaller : IJupyterKernelSpecInstaller
    {
        private readonly IConsole _console;
        private readonly IJupyterKernelSpecModule _kernelSpecModule;

        public JupyterKernelSpecInstaller(IConsole console): this(console, new JupyterKernelSpecModule())
        {
        }

        public JupyterKernelSpecInstaller(IConsole console, IJupyterKernelSpecModule jupyterKernelSpecModule)
        {
            _console = console ?? throw new ArgumentNullException(nameof(console));
            _kernelSpecModule = jupyterKernelSpecModule;
        }


        public async Task<bool> InstallKernel(DirectoryInfo sourceDirectory, DirectoryInfo destination = null)
        {
            var kernelDisplayName = GetKernelDisplayName(sourceDirectory);

            if (destination != null)
            {
                return InstallKernelSpecToDirectory(sourceDirectory, destination, kernelDisplayName);
            }

            try
            {
                var result = await _kernelSpecModule.InstallKernel(sourceDirectory);
                if (result.ExitCode == 0)
                {
                    _console.Out.WriteLine($"Installed \"{kernelDisplayName}\" kernel.");
                    return true;
                }
            }
            catch (Win32Exception w32e)
            {
                // file not found when executing process
                if (!w32e.Source.Contains(typeof(System.Diagnostics.Process).FullName))
                {
                    _console.Error.WriteLine($"Failed installing \"{kernelDisplayName}\" kernel.");
                    throw;
                }
            }

            destination = _kernelSpecModule.GetDefaultKernelSpecDirectory();

            _console.Out.WriteLine("The kernelspec module is not available.");

            return InstallKernelSpecToDirectory(destination, destination, kernelDisplayName);
        }

        private bool InstallKernelSpecToDirectory(DirectoryInfo sourceDirectory, DirectoryInfo destination,
            string kernelDisplayName)
        {
            if (!destination.Exists)
            {
                _console.Error.WriteLine($"The kernelspec path ${destination.FullName} does not exist.");
                _console.Error.WriteLine($"Failed to install \"{kernelDisplayName}\" kernel.");

                return false;
            }
            _console.Out.WriteLine($"Installing using path {destination.FullName}.");
            var succeeded = CopyKernelSpecFiles(sourceDirectory, destination);
            if (succeeded)
            {
                _console.Out.WriteLine($"Installed \"{kernelDisplayName}\" kernel.");
            }
            else
            {
                _console.Error.WriteLine(
                    $"Failed to install \"{kernelDisplayName}\" kernel.");
            }

            return succeeded;
        }

        private string GetKernelDisplayName(DirectoryInfo directory)
        {
            var kernelSpec = directory.GetFiles("kernel.json", SearchOption.AllDirectories).Single();

            var parsed = JObject.Parse(File.ReadAllText(kernelSpec.FullName));
            return parsed["display_name"].Value<string>();
        }

        public async Task<bool> UninstallKernel(DirectoryInfo sourceDirectory)
        {
            if (!sourceDirectory.Exists)
            {
                _console.Error.WriteLine($"Failed to uninstall. The kernelspec path ${sourceDirectory.FullName} does not exist.");

                return false;
            }

            var kernelDisplayName = GetKernelDisplayName(sourceDirectory);
            var commandLineResult = await _kernelSpecModule.UninstallKernel(sourceDirectory);
            
            var result = commandLineResult.ExitCode == 0;

            if (result)
            {
                _console.Out.WriteLine($"Installed \"{kernelDisplayName}\" kernel.");
            }
            else
            {
                _console.Error.WriteLine(
                    $"Failed to uninstall \"{kernelDisplayName}\" kernel.");
            }

            return result;
        }


        private bool CopyKernelSpecFiles(DirectoryInfo source, DirectoryInfo location)
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
                _console.Error.WriteLine($"Directory {location.FullName} does not exists");
                return false;
            }


            try
            {
                var destination = new DirectoryInfo(Path.Combine(location.FullName, source.Name));
                if (destination.Exists)
                {
                    destination.Delete(true);
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
            }
            catch (IOException ioe)
            {
                _console.Error.WriteLine(ioe.Message);
                return false;
            }

            return true;
        }

    }
}
