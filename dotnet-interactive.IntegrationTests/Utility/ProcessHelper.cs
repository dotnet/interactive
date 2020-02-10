using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Microsoft.DotNet.Interactive.App.IntegrationTests.Utility
{
    public static class ProcessHelper
    {
        public static Process Start(
            string command,
            string args,
            DirectoryInfo workingDir,
            Action<string> output = null,
            Action<string> error = null)
        {
            var process = new Process
            {
                StartInfo =
                    {
                        Arguments = args,
                        FileName = command,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        RedirectStandardInput = true,
                        WorkingDirectory = workingDir?.FullName ?? string.Empty,
                        StandardOutputEncoding = Encoding.UTF8
                    }
            };

            if (output != null)
            {
                process.OutputDataReceived += (sender, eventArgs) =>
                {
                    if (eventArgs.Data != null)
                    {
                        output(eventArgs.Data);
                    }
                };
            }

            if (error != null)
            {
                process.ErrorDataReceived += (sender, eventArgs) =>
                {
                    if (eventArgs.Data != null)
                    {
                        error(eventArgs.Data);
                    }
                };
            }

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }
    }
}
