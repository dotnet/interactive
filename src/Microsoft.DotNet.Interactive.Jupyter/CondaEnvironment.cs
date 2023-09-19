using Microsoft.DotNet.Interactive.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter;

internal class CondaEnvironment : IJupyterEnvironment
{
    private class EnvironmentListResults
    {
        public string[] envs { get; set; }
    }

    public const string BASE_ENV = "base";
    public string Name { get; set; }
    public static string CondaPath;
    private static IReadOnlyCollection<string> _environments = new List<string>();

    static CondaEnvironment()
    {
        CondaPath = GetCondaPath();
        Task.Run(async () => _environments = await GetEnvironmentsAsync());
    }

    public CondaEnvironment(string name = BASE_ENV)
    {
        Name = name;
    }

    public static IReadOnlyCollection<string> GetEnvironments()
    {
        return _environments;
    }

    private static async Task<IReadOnlyCollection<string>> GetEnvironmentsAsync()
    {
        var envList = await Execute("conda", "env list --json");
        if (envList.ExitCode == 0)
        {
            var results = JsonSerializer.Deserialize<EnvironmentListResults>(string.Join(string.Empty, envList.Output));
            return results.envs.Select(e =>
            {
                if (e.Contains("\\envs\\"))
                {
                    return e.Split("\\").LastOrDefault();
                }
                else
                {
                    return BASE_ENV;
                }
            }).ToList();
        }

        return _environments;
    }

    private static async Task<CommandLineResult> Execute(string command, string args, string environmentName = BASE_ENV, DirectoryInfo workingDir = null, TimeSpan? timeout = null)
    {
        return await CommandLine.Execute(CondaPath, $"activate {environmentName}&{command} {args}");
    }

    public async Task<CommandLineResult> Execute(string command, string args, DirectoryInfo workingDir = null, TimeSpan? timeout = null)
    {
        return await Execute(command, args, Name, workingDir, timeout);
    }

    public Process StartProcess(string command, string args, DirectoryInfo workingDir, Action<string> output = null, Action<string> error = null)
    {
        return CommandLine.StartProcess(CondaPath, $"activate {Name}&{command} {args}", workingDir, output, error);
    }

    private static string GetCondaPath()
    {
        string condaCommand = GetCondaExecutable();

        string condaPath = Environment.GetEnvironmentVariable("CONDA_PATH");
        if (condaPath != null)
        {
            return Path.Combine(condaPath, condaCommand);
        }

        string pathEnvVar = Environment.GetEnvironmentVariable("Path");
        if (pathEnvVar.Contains("conda"))
        {
            return condaCommand;
        }

        List<string> paths = new List<string>();

        // potential install paths for conda first and then miniconda
        paths.AddRange(new string[] {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "anaconda3", "condabin"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "anaconda3", "Scripts"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "anaconda3", "condabin"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "anaconda3", "Scripts"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "anaconda3", "condabin"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "anaconda3", "Scripts"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "miniconda3", "condabin"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "miniconda3", "Scripts"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "miniconda3", "condabin"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "miniconda3", "Scripts"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "miniconda3", "condabin"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "miniconda3", "Scripts"),
                    });

        return paths.Select(p => Path.Combine(p, condaCommand)).FirstOrDefault(File.Exists);
    }

    private static string GetCondaExecutable()
    {
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.Win32NT:
                return "conda.bat";
            default:
                return "conda";
        }
    }
}
