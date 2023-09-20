// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static Pocket.Logger;

namespace Microsoft.DotNet.Interactive.Jupyter;

public class JupyterKernelSpecModule : IJupyterKernelSpecModule
{
    private readonly IJupyterEnvironment _environment;

    private class KernelSpecListCommandResults
    {
        public Dictionary<string, KernelSpecResourceDetail> kernelspecs { get; set; }
    }

    public JupyterKernelSpecModule(IJupyterEnvironment environment = null)
    {
        _environment = environment ?? new DefaultJupyterEnvironment();
    }
    private class KernelSpecResourceDetail
    {
        public string resource_dir { get; set; }
        public KernelSpec spec { get; set; }
    }

    private async Task<CommandLineResult> ExecuteCommand(string command, string args = "")
    {
        return await _environment.Execute("jupyter", $"kernelspec {command} {args}");
    }

    public Task<CommandLineResult> InstallKernel(DirectoryInfo sourceDirectory)
    {
        return ExecuteCommand($@"install ""{sourceDirectory.FullName}""", "--user");
    }

    public async Task<IReadOnlyDictionary<string, KernelSpec>> ListKernels()
    {
        try
        {
            var kernelSpecsList = await ExecuteCommand("list", "--json");
            if (kernelSpecsList.ExitCode == 0)
            {
                var results = JsonSerializer.Deserialize<KernelSpecListCommandResults>(string.Join(string.Empty, kernelSpecsList.Output));
                return results.kernelspecs?.ToDictionary(r => r.Key, r =>
                {
                    var spec = r.Value?.spec;
                    spec.Name ??= r.Key;
                    return spec;
                });
            }
            else
            {
                // fall back to custom lookup logic 
                return LookupInstalledKernels();
            }
        }
        catch (Exception exception)
        {
            Log.Warning("Failed to retrieve kernel specs", exception);
            // fall back to custom lookup logic 
            return LookupInstalledKernels();
        }
    }

    public DirectoryInfo GetDefaultKernelSpecDirectory()
    {
        var dataDirectory = JupyterCommonDirectories.GetDataDirectory();
        var directory = new DirectoryInfo(Path.Combine(dataDirectory.FullName, "kernels"));
        return directory;
    }

    private IReadOnlyDictionary<string, KernelSpec> LookupInstalledKernels()
    {
        var specs = new Dictionary<string, KernelSpec>();

        var dataDirectories = JupyterCommonDirectories.GetDataDirectories();
        foreach (var directory in dataDirectories)
        {
            var kernelDir = new DirectoryInfo(Path.Combine(directory.FullName, "kernels"));
            if (kernelDir.Exists)
            {
                var kernels = kernelDir.GetDirectories();
                foreach (var kernel in kernels)
                {
                    if (!specs.ContainsKey(kernel.Name))
                    {
                        var spec = GetKernelSpec(kernel);
                        if (spec != null)
                        {
                            specs.Add(spec.Name, spec);
                        }
                    }
                }
            }
        }

        return specs;
    }

    private KernelSpec GetKernelSpec(DirectoryInfo directory)
    {
        var kernelJsonPath = Path.Combine(directory.FullName, "kernel.json");
        if (File.Exists(kernelJsonPath))
        {
            var kernelJson = JsonDocument.Parse(File.ReadAllText(kernelJsonPath));
            var spec = JsonSerializer.Deserialize<KernelSpec>(kernelJson, JsonFormatter.SerializerOptions);
            spec.Name ??= directory.Name;
            return spec;
        }

        return null;
    }

    public IJupyterEnvironment GetEnvironment()
    {
        return _environment;
    }
}