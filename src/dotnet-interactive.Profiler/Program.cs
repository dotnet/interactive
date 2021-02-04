// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using JetBrains.Profiler.Api;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;
using XPlot.DotNet.Interactive.KernelExtensions;

namespace Microsoft.DotNet.Interactive.Profiler
{
    class Program
    {
        static async Task Main(
            Operation operation,
            int iterationCount = 20,
            string kernelName = "csharp")
        {
            MemoryProfiler.CollectAllocations(true);
            MemoryProfiler.ForceGc();

            for (var i = 0; i < iterationCount; i++)
            {
                MemoryProfiler.GetSnapshot($"Before {kernelName} Kernel creation at Iteration {i}");
                MemoryProfiler.ForceGc();

                using var kernel = await CreateKernel(
                                       operation,
                                       kernelName);

                MemoryProfiler.ForceGc();
                MemoryProfiler.GetSnapshot($"Before {kernelName} Iteration {i}");

                await RunProfiledOperation(operation, kernel);

                MemoryProfiler.GetSnapshot($"After {kernelName} Iteration {i}");
                kernel.Dispose();
                MemoryProfiler.ForceGc();
            }
        }

        private static async Task RunProfiledOperation(
            Operation operation,
            CompositeKernel kernel)
        {
            switch (operation)
            {
                case Operation.SubmitCode:
                    var submitCode = CreateSubmitCode(kernel);
                    await kernel.SendAsync(submitCode);
                    break;

                case Operation.RequestCompletion:
                    var requestCompletion = CreateRequestCompletion(kernel);
                    await kernel.SendAsync(requestCompletion);
                    break;
            }
        }

        private static RequestCompletions CreateRequestCompletion(Kernel kernel)
        {
            return new RequestCompletions(
                "aaa",
                new LinePosition(0, 3),
                kernel.Name);
        }

        private static SubmitCode CreateSubmitCode(CompositeKernel kernel)
        {
            switch (kernel.DefaultKernelName)
            {
                case "csharp":
                    return new SubmitCode(@"
Console.Write(""value one"");
Console.Write(""value two"");
Console.Write(""value three"");", "csharp");

                case "fsharp":
                    return new SubmitCode(@"open System
Console.Write(""value one"")
Console.Write(""value two"")
Console.Write(""value three"")", "fsharp");

                default:
                    throw new ArgumentOutOfRangeException($"kernel {kernel.DefaultKernelName} not supported");
            }
        }

        private static async Task<CompositeKernel> CreateKernel(
            Operation operation,
            string kernelName)
        {
            var kernel = new CompositeKernel
                {
                    new CSharpKernel()
                        .UseDefaultFormatting()
                        .UseNugetDirective()
                        .UseKernelHelpers()
                        .UseJupyterHelpers()
                        .UseWho()
                        .UseXplot()
                        .UseMathAndLaTeX(),
                    new FSharpKernel()
                        .UseDefaultFormatting()
                        .UseNugetDirective()
                        .UseKernelHelpers()
                        .UseWho()
                        .UseDefaultNamespaces()
                        .UseXplot()
                        .UseMathAndLaTeX()
                }
                .UseDefaultMagicCommands();

            kernel.DefaultKernelName = kernelName;
            kernel.Name = ".NET";

            switch (operation)
            {
                case Operation.RequestCompletion:

                    var code = kernelName switch
                    {
                        "csharp" => "var aaaaaa = 123;",
                        "fsharp" => "let aaaaaa = 123",
                        _ => throw new ArgumentOutOfRangeException(nameof(kernelName))
                    };

                    await kernel.SendAsync(
                        new SubmitCode(code, targetKernelName: kernelName));
                    break;
            }

            return kernel;
        }
    }
}