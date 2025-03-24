using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests;

public class StdIoBehaviorTests
{
    [Fact]
    public async Task Quit_command_causes_stdio_process_to_end()
    {
        var testProcessDelay = TimeSpan.FromSeconds(5);

        var toolPath = new FileInfo(typeof(Program).Assembly.Location);
        var args = new[]
        {
            $"\"{toolPath.FullName}\"",
            "stdio",
            "--default-kernel",
            "csharp",
        };
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Dotnet.Path.FullName,
                Arguments = string.Join(" ", args),
                EnvironmentVariables =
                {
                    ["DOTNET_INTERACTIVE_SKIP_FIRST_TIME_EXPERIENCE"]  = "1",
                    ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"]  = "1",
                    ["DOTNET_DbgEnableMiniDump"] = "0" // https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dumps
                },
                WorkingDirectory = toolPath.Directory.FullName,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
                UseShellExecute = false,
            },
            EnableRaisingEvents = true,
        };

        var kernelReadyEventTaskCompletionSource = new TaskCompletionSource();
        process.OutputDataReceived += (_, args) =>
        {
            if (args?.Data?.Contains(nameof(KernelReady)) == true)
            {
                kernelReadyEventTaskCompletionSource.SetResult();
            }
        };
        process.Start();
        process.BeginOutputReadLine();

        // wait for kernel ready
        var kernelReadyDelayTask = Task.Delay(testProcessDelay);
        var completedReadyTask = await Task.WhenAny(kernelReadyEventTaskCompletionSource.Task, kernelReadyDelayTask);
        if (!ReferenceEquals(completedReadyTask, kernelReadyEventTaskCompletionSource.Task))
        {
            process.Kill();
            throw new Exception("Child process did not return kernel ready event in time");
        }

        // send quit command
        var quitCommand = new Quit();
        var commandEnvelope = KernelCommandEnvelope.Create(quitCommand);
        var commandJson = KernelCommandEnvelope.Serialize(commandEnvelope);
        await process.StandardInput.WriteLineAsync(commandJson);

        // wait for exit
        var waitForExitTask = process.WaitForExitAsync();
        var delayTask = Task.Delay(TimeSpan.FromSeconds(5));
        var completedTask = await Task.WhenAny(waitForExitTask, delayTask);
        if (!ReferenceEquals(completedTask, waitForExitTask))
        {
            process.Kill();
            throw new Exception("Child process did not end after sending the quit command");
        }
    }
}