using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.IntegrationTests.Utility;
using Microsoft.DotNet.Interactive.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.IntegrationTests
{
    public class LoggingTests
    {
        [Fact]
        public async Task kernel_server_honors_log_path()
        {
            using var logPath = DisposableDirectory.Create();
            using var outputReceived = new ManualResetEvent(false);
            var errorLines = new List<string>();

            // start as external process
            var kernelServerProcess = ProcessHelper.Start(
                command: "dotnet",
                args: $@"interactive kernel-server --log-path ""{logPath.Directory.FullName}""",
                workingDir: new DirectoryInfo(Directory.GetCurrentDirectory()),
                output: _line => { outputReceived.Set(); },
                error: errorLines.Add);

            // wait for log file to be created
            var logFile = await logPath.Directory.WaitForFile(
                timeout: TimeSpan.FromSeconds(2),
                predicate: _file => true); // any matching file is the one we want
            errorLines.Should().BeEmpty();
            logFile.Should().NotBeNull("unable to find created log file");

            // submit code
            var submissionJson = @"{""token"":""abc"",""commandType"":""SubmitCode"",""command"":{""code"":""1+1"",""submissionType"":0,""targetKernelName"":null}}";
            await kernelServerProcess.StandardInput.WriteLineAsync(submissionJson);
            await kernelServerProcess.StandardInput.FlushAsync();

            // wait for output to proceed
            var gotOutput = outputReceived.WaitOne(timeout: TimeSpan.FromSeconds(2));
            gotOutput.Should().BeTrue("expected to receive on stdout");

            // kill
            kernelServerProcess.StandardInput.Close(); // simulate Ctrl+C
            await Task.Delay(TimeSpan.FromSeconds(2)); // allow logs to be flushed
            kernelServerProcess.Kill();
            kernelServerProcess.WaitForExit(2000).Should().BeTrue();
            errorLines.Should().BeEmpty();

            // check log file for expected contents
            (await logFile.WaitForFileCondition(
                timeout: TimeSpan.FromSeconds(2),
                predicate: file => file.Length > 0))
                .Should().BeTrue("expected non-empty log file");
            var logFileContents = File.ReadAllText(logFile.FullName);
            logFileContents.Should().Contain("ℹ OnAssemblyLoad: ");
        }
    }
}
