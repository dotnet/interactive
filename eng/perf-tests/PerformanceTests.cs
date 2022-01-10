using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;

namespace PerfTests
{
    public class PerformanceTests
    {
        private readonly static Version LastVersionWithParseCommand = new Version("1.0.245901");
        private readonly static Version MinVersionWithParserServer = new Version("1.0.250604");

        private readonly static Version ToolVersion = new Version("1.0.260601");

        private Process _process;
        private string _toolDirectory;

        private void RunProcess(params string[] command)
        {
            var psi = new ProcessStartInfo()
            {
                FileName = command[0],
                Arguments = string.Join(" ", command.Skip(1)),
                WorkingDirectory = _toolDirectory,
            };
            var process = Process.Start(psi);
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new Exception($"Process [{string.Join(" ", command)}] exited with code {process.ExitCode}");
            }
        }

        private void StartDotNet(params string[] args)
        {
            var psi = new ProcessStartInfo()
            {
                FileName = "dotnet",
                Arguments = string.Join(" ", args),
                WorkingDirectory = _toolDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            _process = new Process() { StartInfo = psi };
            _process.EnableRaisingEvents = true;
            _process.Start();
            _process.BeginErrorReadLine();
            _process.BeginOutputReadLine();
        }

        [IterationCleanup]
        public void StopDotNet()
        {
            _process.Kill();
        }

        [GlobalSetup]
        public void PrepareTool()
        {
            _toolDirectory = Path.Combine(Directory.GetCurrentDirectory(), "interactive-tool");
            if (Directory.Exists(_toolDirectory))
            {
                Directory.Delete(_toolDirectory, true);
            }

            Directory.CreateDirectory(_toolDirectory);
            RunProcess("dotnet", "new", "tool-manifest");
            RunProcess("dotnet", "tool", "install", "Microsoft.dotnet-interactive", "--version", ToolVersion.ToString());
        }

        [Benchmark]
        public async Task ProcessStartToQuickInfo()
        {
            StartDotNet("interactive", "[vscode]", "stdio");

            var kernelReadyTaskCompletionSource = new TaskCompletionSource();
            var hoverTextProducedTaskCompletionSource = new TaskCompletionSource();
            _process.OutputDataReceived += (o, args) =>
            {
                if (args.Data?.Contains("KernelReady") == true)
                {
                    kernelReadyTaskCompletionSource.SetResult();
                }
                if (args.Data?.Contains("HoverTextProduced") == true)
                {
                    hoverTextProducedTaskCompletionSource.SetResult();
                }
            };

            await kernelReadyTaskCompletionSource.Task;

            var requestObject = new
            {
                commandType = "RequestHoverText",
                command = new
                {
                    code = "var x = 1234;",
                    linePosition = new
                    {
                        line = 0,
                        character = 10,
                    }
                }
            };
            var requestText = JsonConvert.SerializeObject(requestObject);
            _process.StandardInput.WriteLine(requestText);
            await hoverTextProducedTaskCompletionSource.Task;
        }

        [Benchmark]
        public async Task ProcessStartToParseNotebook()
        {
            var isDedicatedParserServer = ToolVersion >= MinVersionWithParserServer;

            if (isDedicatedParserServer)
            {
                StartDotNet("interactive", "notebook-parser");
            }
            else
            {
                StartDotNet("interactive", "[vscode]", "stdio");
            }

            var kernelReadyTaskCompletionSource = new TaskCompletionSource();
            var notebookParsedTaskCompletionSource = new TaskCompletionSource();
            _process.OutputDataReceived += (o, args) =>
            {
                if (args.Data?.Contains("KernelReady") == true)
                {
                    kernelReadyTaskCompletionSource.SetResult();
                }
                if (isDedicatedParserServer)
                {
                    if (args.Data?.Contains("document") == true)
                    {
                        notebookParsedTaskCompletionSource.SetResult();
                    }
                }
                else
                {
                    if (args.Data?.Contains("InteractiveDocumentParsed") == true)
                    {
                        notebookParsedTaskCompletionSource.SetResult();
                    }
                }
            };

            if (!isDedicatedParserServer)
            {
                await kernelReadyTaskCompletionSource.Task;
            }

            var parserServerRequestObject = new
            {
                type = "parse",
                id = "1",
                serializationType = "dib",
                defaultLanguage = "dotnet-interactive.csharp",
                rawData = "",
            };
            var parserServerRequestText = JsonConvert.SerializeObject(parserServerRequestObject);
            var parseCommandObject = new
            {
                commandType = "ParseInteractiveDocument",
                command = new
                {
                    fileName = "notebook.dib",
                    rawData = "",
                    targetKernelName = ".NET",
                }

            };
            var parseCommandText = JsonConvert.SerializeObject(parseCommandObject);

            var requestText = isDedicatedParserServer ? parserServerRequestText : parseCommandText;

            _process.StandardInput.WriteLine(requestText);
            await notebookParsedTaskCompletionSource.Task;
        }
    }
}
