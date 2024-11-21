using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Jupyter;

public interface IJupyterEnvironment
{
    /// <summary>
    ///  Executes the command in the activated jupyter environment
    /// </summary>
    Task<CommandLineResult> ExecuteAsync(
        string command,
        string args,
        DirectoryInfo workingDir = null,
        TimeSpan? timeout = null);

    /// <summary>
    /// Starts the process in the activated jupyter environment
    /// </summary>
    Process StartProcess(
        string command,
        string args,
        DirectoryInfo workingDir,
        Action<string> output = null,
        Action<string> error = null);
}