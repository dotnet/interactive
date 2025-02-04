// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Pocket;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Jupyter.JupyterKernelSpecModule>;

namespace Microsoft.DotNet.Interactive.Jupyter;

public class JupyterKernelSpecModule : IJupyterKernelSpecModule
{
    private readonly IJupyterEnvironment _environment;

    private class KernelSpecListCommandResult
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

    private async Task<CommandLineResult> ExecuteCommandAsync(string command, string args = "")
    {
        return await _environment.ExecuteAsync("jupyter", $"kernelspec {command} {args}");
    }

    public Task<CommandLineResult> InstallKernelAsync(DirectoryInfo sourceDirectory)
    {
        return ExecuteCommandAsync($"""
                               install "{sourceDirectory.FullName}"
                               """, "--user");
    }

    public async Task<IReadOnlyDictionary<string, KernelSpec>> ListKernelsAsync()
    {
        try
        {
            var commandLineResult = await _environment.ExecuteAsync("jupyter", "kernelspec list --json");

            if (commandLineResult.ExitCode is 0)
            {
                var json = string.Join(string.Empty, commandLineResult.Output);
                var results = JsonSerializer.Deserialize<KernelSpecListCommandResult>(json);

                if (results.kernelspecs is not null)
                {
                    return results.kernelspecs?.ToDictionary(
                        r => r.Key,
                        r =>
                        {
                            var spec = r.Value?.spec;
                            spec.Name ??= r.Key;
                            return spec;
                        });
                }
            }
            else
            {
                Log.Warning("Failed to list kernelspecs.", commandLineResult.Error);
            }

            // fall back to custom lookup logic 
            return FindInstalledKernels();
        }
        catch (Exception exception)
        {
            Log.Warning("Failed to list kernelspecs", exception);
            // fall back to custom lookup logic 
            return FindInstalledKernels();
        }
    }

    public DirectoryInfo GetDefaultKernelSpecDirectory()
    {
        var dataDirectory = JupyterCommonDirectories.GetDataDirectory();
        var directory = new DirectoryInfo(Path.Combine(dataDirectory.FullName, "kernels"));
        return directory;
    }

    private IReadOnlyDictionary<string, KernelSpec> FindInstalledKernels()
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
                        if (spec is not null)
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
            var spec = kernelJson.Deserialize<KernelSpec>(JsonFormatter.SerializerOptions);
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