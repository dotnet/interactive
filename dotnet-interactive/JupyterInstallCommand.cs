// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.Utility;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.App
{
    public class JupyterInstallCommand
    {
        private readonly IConsole _console;
        private readonly IJupyterKernelSpec _jupyterKernelSpec;
        private readonly PortRange _httpPortRange;
        private readonly DirectoryInfo _location;

        public JupyterInstallCommand(IConsole console, IJupyterKernelSpec jupyterKernelSpec, PortRange httpPortRange = null, DirectoryInfo location = null)
        {
            _console = console;
            _jupyterKernelSpec = jupyterKernelSpec;
            _httpPortRange = httpPortRange;
            _location = location;
        }

        public async Task<int> InvokeAsync()
        {
            using (var disposableDirectory = DisposableDirectory.Create())
            {
                var assembly = typeof(Program).Assembly;

                using (var resourceStream = assembly.GetManifestResourceStream("dotnetKernel.zip"))
                {
                    var zipPath = Path.Combine(disposableDirectory.Directory.FullName, "dotnetKernel.zip");

                    using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
                    {
                        resourceStream.CopyTo(fileStream);
                    }

                    var dotnetDirectory = disposableDirectory.Directory;
                    ZipFile.ExtractToDirectory(zipPath, dotnetDirectory.FullName);

                    if (_httpPortRange != null)
                    {
                        ComputeKernelSpecArgs(_httpPortRange, dotnetDirectory);
                    }
                    var installErrors = 0;

                    if (_location != null)
                    {
                        _console.Out.WriteLine($"Installing kernels at location {_location.FullName}");
                    }
                    else if (_jupyterKernelSpec.CanInstall)
                    {
                        _console.Out.WriteLine("Installing kernels using jupyter kernelspec module");
                    }
                    else
                    {
                        _console.Out.WriteLine("jupyter kernelspec module is not available, installing kernels default directory");
                    }


                    foreach (var kernelDirectory in dotnetDirectory.GetDirectories())
                    {
                        if (_location != null)
                        {
                            var errors = InstallKernelToLocation(kernelDirectory, _location);
                            installErrors += errors;
                        }
                        else if (_jupyterKernelSpec.CanInstall)
                        {
                            var result = await _jupyterKernelSpec.InstallKernel(kernelDirectory);
                            if (result.ExitCode == 0)
                            {
                                _console.Out.WriteLine(string.Join('\n', result.Output));
                                _console.Out.WriteLine(string.Join('\n', result.Error));
                                _console.Out.WriteLine(".NET kernel installation succeeded");
                            }
                            else
                            {
                                _console.Error.WriteLine(
                                    $".NET kernel installation failed with error: {string.Join('\n', result.Error)}");
                                installErrors++;
                            }
                        }
                        else
                        {
                            var location = GetDefaultDirectory();
                            var errors = InstallKernelToLocation(kernelDirectory, location);
                            installErrors += errors;
                        }
                    }

                    return installErrors;
                }
            }
        }

        private int InstallKernelToLocation(DirectoryInfo source, DirectoryInfo location)
        {
            var installErrors = 0;
            var (succeeded, outputs, errors) = CopyKernelSpecFiles(source, location);

            if (succeeded)
            {
                _console.Out.WriteLine(string.Join('\n', outputs));
                _console.Out.WriteLine(string.Join('\n', errors));
                _console.Out.WriteLine(".NET kernel installation succeeded");
            }
            else
            {
                _console.Error.WriteLine(
                    $".NET kernel installation failed with error: {string.Join('\n', errors)}");
                installErrors++;
            }

            return installErrors;
        }

        private (bool, IEnumerable<string> outputs, IEnumerable<string> errors) CopyKernelSpecFiles(DirectoryInfo source, DirectoryInfo location)
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
                return (false, Enumerable.Empty<string>(), new[] {$"Directory {location.FullName} does not exists"});
            }

            var outputs = new List<string>();
            var errors = new List<string>();
            var success = true;
            var destination = new DirectoryInfo(Path.Combine(location.FullName, source.Name));

            try
            {
                if (destination.Exists)
                {
                    destination.Delete(true);
                    outputs.Add($"[InstallKernelSpec] Removing existing kernelspec in {destination.FullName}");
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


                outputs.Add($"[InstallKernelSpec] Installed kernelspec {source.Name} in {destination.FullName}");

            }
            catch (IOException ioe)
            {
                success = false;
                errors.Add(ioe.Message);
            }

            return (success, outputs, errors);
        }

        private DirectoryInfo GetDefaultDirectory()
        {
            DirectoryInfo directory;
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                    directory =  new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"jupyter","kernels"));
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

        private static void ComputeKernelSpecArgs(PortRange httpPortRange, DirectoryInfo directory)
        {
            var kernelSpecs = directory.GetFiles("kernel.json", SearchOption.AllDirectories);

            foreach (var kernelSpec in kernelSpecs)
            {
                var parsed = JObject.Parse(File.ReadAllText(kernelSpec.FullName));

                var argv = parsed["argv"].Value<JArray>();

                argv.Insert(argv.Count -1, "--http-port-range");
                argv.Insert(argv.Count - 1, $"{httpPortRange.Start}-{httpPortRange.End}");

                File.WriteAllText(kernelSpec.FullName, parsed.ToString(Newtonsoft.Json.Formatting.Indented));

            }
        }
    }
}