// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.DotNet.Interactive.Telemetry
{
    internal static class ProcessStartInfoExtensions
    {
        public static int ExecuteAndCaptureOutput(this ProcessStartInfo startInfo, out string stdOut, out string stdErr)
        {
            var outStream = new StreamForwarder().Capture();
            var errStream = new StreamForwarder().Capture();

            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            using var process = new Process
            {
                StartInfo = startInfo
            };

            process.EnableRaisingEvents = true;

            process.Start();

            var taskOut = outStream.BeginRead(process.StandardOutput);
            var taskErr = errStream.BeginRead(process.StandardError);

            process.WaitForExit();

            taskOut.Wait();
            taskErr.Wait();

            stdOut = outStream.CapturedOutput;
            stdErr = errStream.CapturedOutput;

            return process.ExitCode;
        }
    }
}
