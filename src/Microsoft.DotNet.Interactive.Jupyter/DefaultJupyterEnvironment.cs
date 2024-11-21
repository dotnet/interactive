using Microsoft.DotNet.Interactive.Utility;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Jupyter;

internal class DefaultJupyterEnvironment : IJupyterEnvironment
{
    public async Task<CommandLineResult> ExecuteAsync(string command, string args, DirectoryInfo workingDir = null, TimeSpan? timeout = null)
    {
        return await CommandLine.Execute(command, args, workingDir, timeout);
    }

    public Process StartProcess(string command, string args, DirectoryInfo workingDir, Action<string> output = null, Action<string> error = null)
    {
        // FIX: (StartProcess) make sure these are getting disposed
        return CommandLine.StartProcess(command, args, workingDir, output, error);
    }
}
