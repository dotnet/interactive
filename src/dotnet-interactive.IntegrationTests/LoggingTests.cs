// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.IntegrationTests.Utility;
using Microsoft.DotNet.Interactive.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.App.IntegrationTests
{
    public class LoggingTests
    {
        private readonly ITestOutputHelper _output;

        public LoggingTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [IntegrationFact]
        public async Task kernel_server_honors_log_path()
        {
            using var logPath = DisposableDirectory.Create();

            _output.WriteLine($"Created log file: {logPath.Directory.FullName}");

            using var outputReceived = new ManualResetEvent(false);
            var outputLock = new object();
            var receivedOutput = false;
            var errorLines = new List<string>();
            var waitTime = TimeSpan.FromSeconds(10);

            // start as external process
            var kernelServerProcess = ProcessHelper.Start(
                command: "dotnet",
                args: $@"interactive stdio --log-path ""{logPath.Directory.FullName}"" --verbose",
                workingDir: new DirectoryInfo(Directory.GetCurrentDirectory()),
                output: _line =>
                {
                    lock (outputLock)
                    {
                        if (!receivedOutput)
                        {
                            receivedOutput = true;
                            outputReceived.Set();
                        }
                    }
                },
                error: errorLines.Add);

            // wait for log file to be created
            var logFile = await logPath.Directory.WaitForFile(
                              timeout: waitTime,
                              predicate: _file => true); // any matching file is the one we want
            errorLines.Should().BeEmpty("there should not be any errors");
            logFile.Should().NotBeNull($"a log file should have been created at {logFile.FullName}");

            // submit code
            var submissionJson = @"{""token"":""abc"",""commandType"":""SubmitCode"",""command"":{""code"":""1+1"",""submissionType"":0,""targetKernelName"":null}}";
            await kernelServerProcess.StandardInput.WriteLineAsync(submissionJson);
            await kernelServerProcess.StandardInput.FlushAsync();

            // wait for output to proceed
            var gotOutput = outputReceived.WaitOne(timeout: TimeSpan.FromSeconds(4));
            gotOutput.Should().BeTrue("expected to receive on stdout");

            // kill
            await Task.Delay(TimeSpan.FromSeconds(4)); // allow logs to be flushed
            kernelServerProcess.Kill();
            kernelServerProcess.WaitForExit(2000).Should().BeTrue("process should exit quickly");
            errorLines.Should().BeEmpty();

            // check log file for expected contents
            (await logFile.WaitForFileCondition(
                 timeout: waitTime,
                 predicate: file => file.Length > 0))
                .Should()
                .BeTrue($"expected non-empty log file within {waitTime.TotalSeconds}s");
            var logFileContents = File.ReadAllText(logFile.FullName);
            logFileContents.Should().Contain("CodeSubmissionReceived: 1+1");
        }
    }
}
