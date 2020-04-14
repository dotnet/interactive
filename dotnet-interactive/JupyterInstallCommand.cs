// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.Utility;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.App
{

    public class JupyterInstallCommand
    {
        private readonly IConsole _console;
        private readonly IJupyterKernelSpecInstaller _jupyterKernelSpecInstaller;
        private readonly PortRange _httpPortRange;
        private readonly DirectoryInfo _path;

        public JupyterInstallCommand(IConsole console, IJupyterKernelSpecInstaller jupyterKernelSpecInstaller, PortRange httpPortRange = null, DirectoryInfo path = null)
        {
            _console = console;
            _jupyterKernelSpecInstaller = jupyterKernelSpecInstaller;
            _httpPortRange = httpPortRange;
            _path = path;
        }

        public async Task<int> InvokeAsync()
        {
            var errorCount = 0;
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
                   

                    foreach (var kernelSpecSourcePath in dotnetDirectory.GetDirectories())
                    {
                        var succeeded = await _jupyterKernelSpecInstaller.InstallKernel(kernelSpecSourcePath, _path);
                        
                        if (!succeeded)
                        {
                            errorCount++;
                        }

                    }
                }
            }

            return errorCount;
        }

        private static void ComputeKernelSpecArgs(PortRange httpPortRange, DirectoryInfo directory)
        {
            var kernelSpecs = directory.GetFiles("kernel.json", SearchOption.AllDirectories);

            foreach (var kernelSpec in kernelSpecs)
            {
                var parsed = JObject.Parse(File.ReadAllText(kernelSpec.FullName));

                var argv = parsed["argv"].Value<JArray>();

                argv.Insert(argv.Count - 1, "--http-port-range");
                argv.Insert(argv.Count - 1, $"{httpPortRange.Start}-{httpPortRange.End}");

                File.WriteAllText(kernelSpec.FullName, parsed.ToString(Newtonsoft.Json.Formatting.Indented));

            }
        }
    }
}