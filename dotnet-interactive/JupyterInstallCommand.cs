// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.App
{
    public class JupyterInstallCommand
    {
        private readonly IConsole _console;
        private readonly IJupyterKernelSpec _jupyterKernelSpec;

        public JupyterInstallCommand(IConsole console, IJupyterKernelSpec jupyterKernelSpec)
        {
            _console = console;
            _jupyterKernelSpec = jupyterKernelSpec;
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

                    var installErrors = 0;
                    foreach (var kernelDirectory in dotnetDirectory.GetDirectories())
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
                            _console.Error.WriteLine($".NET kernel installation failed with error: {string.Join('\n', result.Error)}");
                            installErrors++;
                        }
                    }

                    return installErrors;
                }
            }
        }
    }
}