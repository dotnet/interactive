// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Pocket;

namespace Microsoft.DotNet.Interactive.Utility
{
    public static class CommandLine
    {
        public static Task<CommandLineResult> Execute(
            FileInfo exePath,
            string args,
            DirectoryInfo workingDir = null) =>
            Execute(exePath.FullName,
                    args,
                    workingDir);

        public static async Task<CommandLineResult> Execute(
            string command,
            string args,
            DirectoryInfo workingDir = null,
            TimeSpan? timeout = null)
        {
            args ??= "";

            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();

            using (var operation = ConfirmOnExit(command, args))
            using (var process = StartProcess(
                command,
                args,
                workingDir,
                output: data =>
                {
                    stdOut.AppendLine(data);
                },
                error: data =>
                {
                    stdErr.AppendLine(data);
                }))
            {
                var exitCode = await process.Complete().Timeout(timeout ?? TimeSpan.FromMinutes(1));

                var output = stdOut.Replace("\r\n", "\n").ToString().Split('\n');

                var error = stdErr.Replace("\r\n", "\n").ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (error.All(string.IsNullOrWhiteSpace))
                {
                    error = null;
                }

                var result = new CommandLineResult(
                    exitCode: exitCode,
                    output: output,
                    error: error);

                if (exitCode == 0)
                {
                    operation.Succeed(
                        "> {command} {args} -> exited with {code}",
                        process.StartInfo.FileName,
                        process.StartInfo.Arguments,
                        process.ExitCode);
                }
                else
                {
                    operation.Fail(new CommandLineInvocationException(result));
                }

                return result;
            }
        }

        public static async Task<int> Complete(
            this Process process) =>
            await Task.Run(() =>
                      {
                          process.WaitForExit();

                          return process.ExitCode;
                      });

        public static async Task<T> Timeout<T>(
            this Task<T> source,
            TimeSpan timeout)
        {
            if (await Task.WhenAny(
                    source,
                    Task.Delay(timeout)) != source)
            {
                throw new TimeoutException();
            }

            return await source;
        }

        public static Process StartProcess(
            string command,
            string args,
            DirectoryInfo workingDir,
            Action<string> output = null,
            Action<string> error = null,
            params (string key, string value)[] environmentVariables)
        {
            using (var operation = Log.OnEnterAndExit())
            {
                args ??= "";

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

                operation.Info("> {process} {args}", command, args);

                if (environmentVariables?.Length > 0)
                {
                    foreach (var tuple in environmentVariables)
                    {
                        operation.Trace("Adding environment variable {tuple}", tuple);
                        process.StartInfo.Environment.Add(tuple.key, tuple.value);
                    }
                }

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

        public static string AppendArgs(this string initial, string append = null) =>
            string.IsNullOrWhiteSpace(append)
                ? initial
                : $"{initial} {append}";

        private static ConfirmationLogger ConfirmOnExit(
            object command,
            string args,
            [CallerMemberName] string operationName = null)
        {
            return new ConfirmationLogger(
                operationName: operationName,
                category: Log.Category,
                message: "> {command} {args}",
                args: new[] { command, args },
                logOnStart: true);
        }

        private static Logger Log { get; } = new Logger(nameof(CommandLine));
    }
}
