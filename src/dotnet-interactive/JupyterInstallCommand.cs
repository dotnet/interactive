// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.App;

public class JupyterInstallCommand
{
    private readonly IJupyterKernelSpecInstaller _jupyterKernelSpecInstaller;
    private readonly HttpPortRange _httpPortRange;
    private readonly DirectoryInfo _path;
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public JupyterInstallCommand(
        IJupyterKernelSpecInstaller jupyterKernelSpecInstaller, 
        HttpPortRange httpPortRange = null, 
        DirectoryInfo path = null)
    {
        _jupyterKernelSpecInstaller = jupyterKernelSpecInstaller;
        _httpPortRange = httpPortRange;
        _path = path;
    }

    public async Task<int> InvokeAsync()
    {
        var assembly = typeof(Program).Assembly;

        using var disposableDirectory = DisposableDirectory.Create();
        await using var resourceStream = assembly.GetManifestResourceStream("dotnetKernel.zip");

        var zipPath = Path.Combine(disposableDirectory.Directory.FullName, "dotnetKernel.zip");

        using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
        {
            resourceStream.CopyTo(fileStream);
        }

        var dotnetDirectory = disposableDirectory.Directory;
        ZipFile.ExtractToDirectory(zipPath, dotnetDirectory.FullName);

        if (_httpPortRange is not null)
        {
            ComputeKernelSpecArgs(_httpPortRange, dotnetDirectory);
        }

        var errorCount = 0;

        foreach (var kernelSpecSourcePath in dotnetDirectory.GetDirectories())
        {
            var succeeded = await _jupyterKernelSpecInstaller.TryInstallKernelAsync(kernelSpecSourcePath, _path);
                        
            if (!succeeded)
            {
                errorCount++;
            }
        }

        return errorCount;
    }

    private static void ComputeKernelSpecArgs(HttpPortRange httpPortRange, DirectoryInfo directory)
    {
        var kernelSpecs = directory.GetFiles("kernel.json", SearchOption.AllDirectories);

        foreach (var kernelSpec in kernelSpecs)
        {
            var newKernelSpec = JsonDocument.Parse(File.ReadAllText(kernelSpec.FullName)).RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                
            var argv = newKernelSpec["argv"].EnumerateArray().Select(e => e.GetString()).ToList();

            argv.Add( "--http-port-range");
            argv.Add($"{httpPortRange.Start}-{httpPortRange.End}");

            newKernelSpec["argv"] = JsonSerializer.SerializeToElement(argv);

            File.WriteAllText(kernelSpec.FullName, JsonSerializer.Serialize(newKernelSpec, JsonSerializerOptions));

        }
    }
}