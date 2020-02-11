// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using JetBrains.Profiler.Api;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;

namespace Microsoft.DotNet.Interactive.Profiler
{
    class Program
    {
        static async Task Main(string[] args)
        {
            MemoryProfiler.CollectAllocations(true);
            MemoryProfiler.ForceGc();

            const int iterationCount = 20;
            foreach (var kernelName in new []{"csharp", "fsharp"})
            {
                for (int i = 0; i < iterationCount; i++)
                {
                    MemoryProfiler.GetSnapshot($"Before {kernelName} Kernel creation at Iteration {i}");
                    MemoryProfiler.ForceGc();
                    var kernel = CreateKernel(kernelName);
                    MemoryProfiler.ForceGc();
                    MemoryProfiler.GetSnapshot($"Before {kernelName} Iteration {i}");

                    var submitCode = CreateSubmitCode(kernelName);

                    await kernel.SendAsync(submitCode);
                    MemoryProfiler.GetSnapshot($"After {kernelName} Iteration {i}");
                    kernel.Dispose();
                    kernel = null;
                    MemoryProfiler.ForceGc();
                }
            }
           
        }

        private static SubmitCode CreateSubmitCode(string kernelName)
        {
            switch (kernelName)
            {
                case "csharp":
                    return  new SubmitCode(@"
Console.Write(""value one"");
Console.Write(""value two"");
Console.Write(""value three"");"
                        , targetKernelName: kernelName);
                case "fsharp":
                    return new SubmitCode(@"open System
Console.Write(""value one"")
Console.Write(""value two"")
Console.Write(""value three"")"
                        , targetKernelName: kernelName);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kernelName), $"kernel {kernelName} not supported");
            }


        }

        private static IKernel CreateKernel(string kernelName)
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
                        .UseKernelHelpers()
                        .UseWho()
                        .UseDefaultNamespaces()
                        .UseXplot()
                        .UseMathAndLaTeX()
                }
                .UseDefaultMagicCommands();

            kernel.DefaultKernelName = kernelName;
            kernel.Name = ".NET";
            return kernel;
        }
    }
}
