// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.PowerShell;
using Pocket;
using Recipes;
using Xunit.Abstractions;
using Serilog.Sinks.RollingFileAlternate;
using SerilogLoggerConfiguration = Serilog.LoggerConfiguration;

namespace Microsoft.DotNet.Interactive.Tests
{
    [LogTestNamesToPocketLogger]
    public abstract class LanguageKernelTestBase : IDisposable
    {
        static LanguageKernelTestBase()
        {
            var artifactsPath = new DirectoryInfo(".");

            while (artifactsPath.Name != "artifacts")
            {
                if (artifactsPath.Parent != null)
                {
                    artifactsPath = artifactsPath.Parent;
                }
                else
                {
                    break;
                }
            }

            var logPath =
                artifactsPath.Name == "artifacts"
                    ? Path.Combine(
                        artifactsPath.ToString(),
                        "log",
                        "Release")
                    : ".";

            var log = new SerilogLoggerConfiguration()
                      .WriteTo
                      .RollingFileAlternate(logPath, outputTemplate: "{Message}{NewLine}")
                      .CreateLogger();

            LogEvents.Subscribe(
                e => log.Information(e.ToLogString()));
        }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        
        private static readonly AsyncLock _lock = new AsyncLock();
        private readonly AsyncLock.Releaser _lockReleaser;

        protected LanguageKernelTestBase(ITestOutputHelper output)
        {
            _lockReleaser = Task.Run(() => _lock.LockAsync()).Result;

            DisposeAfterTest(output.SubscribeToPocketLogger());
        }
        
        public void Dispose()
        {
            _disposables?.Dispose();

            _lockReleaser.Dispose();
        }

        protected KernelBase CreateKernel(Language language)
        {
            var kernelBase = language switch
            {
                Language.FSharp => new FSharpKernel()
                                   .UseDefaultFormatting()
                                   .UseKernelHelpers()
                                   .UseWho()
                                   .UseDefaultNamespaces() as KernelBase,
                Language.CSharp => new CSharpKernel()
                                   .UseDefaultFormatting()
                                   .UseNugetDirective()
                                   .UseKernelHelpers()
                                   .UseWho(),
                Language.PowerShell => new PowerShellKernel(),
                _ => throw new InvalidOperationException("Unknown language specified")
            };
            
            kernelBase = kernelBase
                .LogEventsToPocketLogger();

            var kernel =
                new CompositeKernel { kernelBase }
                    .UseDefaultMagicCommands(); 

            kernel.DefaultKernelName = kernelBase.Name;

            KernelEvents = kernel.KernelEvents.ToSubscribedList();

            DisposeAfterTest(KernelEvents);
            DisposeAfterTest(kernel);

            return kernel;
        }

        protected KernelBase CreateKernel()
        {
            return CreateKernel(Language.CSharp);
        }

        public async Task SubmitCode(KernelBase kernel, string[] codeFragments, SubmissionType submissionType = SubmissionType.Run)
        {
            foreach (var codeFragment in codeFragments)
            {
                var cmd = new SubmitCode(codeFragment, submissionType: submissionType);
                await kernel.SendAsync(cmd);
            }
        }

        public async Task SubmitCode(KernelBase kernel, string codeFragment, SubmissionType submissionType = SubmissionType.Run)
        {
            var command = new SubmitCode(codeFragment, submissionType: submissionType);
            await kernel.SendAsync(command);
        }

        protected SubscribedList<IKernelEvent> KernelEvents { get; private set; }

        protected void DisposeAfterTest(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }
        
        protected void DisposeAfterTest(Action action)
        {
            _disposables.Add(action);
        }
    }
}
