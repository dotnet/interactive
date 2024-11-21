// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Pocket;
using static System.Environment;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Jupyter.CondaEnvironment>;
using CommandLine = Microsoft.DotNet.Interactive.Utility.CommandLine;

namespace Microsoft.DotNet.Interactive.Jupyter;

internal class CondaEnvironment : IJupyterEnvironment
{
    public const string BASE_ENV = "base";

    private static IReadOnlyCollection<string> _environmentNames = null;

    static CondaEnvironment()
    {
        CondaPath = GetCondaPath();
    }

    public static string CondaPath { get; }

    public CondaEnvironment(string name = BASE_ENV)
    {
        Name = name;
    }

    public string Name { get; set; }

    public static async Task<IReadOnlyCollection<string>> GetEnvironmentNamesAsync() =>
        _environmentNames ??= await DiscoverEnvironmentNamesAsync();

    private static async Task<IReadOnlyCollection<string>> DiscoverEnvironmentNamesAsync()
    {
        var commandLineResult = await ExecuteAsync("conda", "env list --json");

        if (commandLineResult.ExitCode is 0)
        {
            var results = JsonSerializer.Deserialize<EnvironmentListResult>(string.Join(string.Empty, commandLineResult.Output));

            var envNames = results.envs.Select(e =>
            {
                if (e.Contains($"{Path.DirectorySeparatorChar}envs{Path.DirectorySeparatorChar}"))
                {
                    var exeName = e.Split(Path.DirectorySeparatorChar).LastOrDefault();
                    return exeName;
                }
                else
                {
                    return BASE_ENV;
                }
            }).ToArray();

            return envNames;
        }
        else
        {
            Log.Warning("Failed to list Conda environments.", commandLineResult.Error);
            return [BASE_ENV];
        }
    }

    internal static async Task<CommandLineResult> ExecuteAsync(string command, string args, string environmentName = BASE_ENV)
    {
        return await CommandLine.Execute(CondaPath, $"activate {environmentName}&{command} {args}");
    }

    public async Task<CommandLineResult> ExecuteAsync(string command, string args, DirectoryInfo workingDir = null, TimeSpan? timeout = null)
    {
        return await ExecuteAsync(command, args, Name);
    }

    public Process StartProcess(string command, string args, DirectoryInfo workingDir, Action<string> output = null, Action<string> error = null)
    {
        return CommandLine.StartProcess(CondaPath, $"activate {Name}&{command} {args}", workingDir, output, error);
    }

    private static string GetCondaPath()
    {
        string condaExecutableName = OSVersion.Platform switch
        {
            PlatformID.Win32S or PlatformID.Win32Windows or PlatformID.Win32NT => "conda.bat",
            _ => "conda"
        };

        if (GetEnvironmentVariable("CONDA_PATH") is {} envVar_CONDA_PATH)
        {
            Log.Info("Using environment variable CONDA_PATH for Conda at {path}", envVar_CONDA_PATH);
            return Path.Combine(envVar_CONDA_PATH, condaExecutableName);
        }

        if (GetEnvironmentVariable("PATH") is { } envVar_PATH &&
            envVar_PATH.Contains("conda"))
        {
            Log.Info("Found Conda on path: {path}", envVar_PATH);
            // QUESTION: (GetCondaPath) This seems like it could be less robust and/or vary in behavior from the fully qualified paths produced by the other code paths in this method.
            return condaExecutableName;
        }

        // potential install paths for conda first and then miniconda
        string[] searchDirectories =
        [
            Path.Combine(GetFolderPath(SpecialFolder.LocalApplicationData), "anaconda3", "condabin"),
            Path.Combine(GetFolderPath(SpecialFolder.LocalApplicationData), "anaconda3", "Scripts"),
            Path.Combine(GetFolderPath(SpecialFolder.UserProfile), "anaconda3", "condabin"),
            Path.Combine(GetFolderPath(SpecialFolder.UserProfile), "anaconda3", "Scripts"),
            Path.Combine(GetFolderPath(SpecialFolder.System), "anaconda3", "condabin"),
            Path.Combine(GetFolderPath(SpecialFolder.System), "anaconda3", "Scripts"),
            Path.Combine(GetFolderPath(SpecialFolder.LocalApplicationData), "miniconda3", "condabin"),
            Path.Combine(GetFolderPath(SpecialFolder.LocalApplicationData), "miniconda3", "Scripts"),
            Path.Combine(GetFolderPath(SpecialFolder.UserProfile), "miniconda3", "condabin"),
            Path.Combine(GetFolderPath(SpecialFolder.UserProfile), "miniconda3", "Scripts"),
            Path.Combine(GetFolderPath(SpecialFolder.System), "miniconda3", "condabin"),
            Path.Combine(GetFolderPath(SpecialFolder.System), "miniconda3", "Scripts")
        ];

        var path = searchDirectories
                   .Select(p => Path.Combine(p, condaExecutableName))
                   .FirstOrDefault(File.Exists);

        if (path is not null)
        {
            Log.Info("Discovered Conda path: {path}", path);
        }
        else
        {
            Log.Warning("Conda not found.");
        }

        return path;
    }

    private class EnvironmentListResult
    {
        public string[] envs { get; init; }
    }
}
