// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Recipes;
using Xunit.Abstractions;
using Serilog.Sinks.RollingFileAlternate;
using SerilogLoggerConfiguration = Serilog.LoggerConfiguration;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Jupyter;

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

        protected CompositeKernel CreateCompositeKernel(Language defaultKernelLanguage = Language.CSharp)
        {
            return CreateCompositeKernel(
                new[]
                {
                    CreateFSharpKernel(),
                    CreateCSharpKernel(),
                    CreatePowerShellKernel()
                },
                defaultKernelLanguage);
        }

        protected CompositeKernel CreateKernel(Language defaultLanguage = Language.CSharp)
        {
            var languageKernel = defaultLanguage switch
            {
                Language.FSharp => CreateFSharpKernel(),
                Language.CSharp => CreateCSharpKernel(),
                Language.PowerShell => CreatePowerShellKernel(),
                _ => throw new InvalidOperationException($"Unknown language specified: {defaultLanguage}")
            };

            return CreateCompositeKernel(new[] { languageKernel }, defaultLanguage);
        }

        private CompositeKernel CreateCompositeKernel(IEnumerable<Kernel> subkernels, Language defaultKernelLanguage)
        {
            var kernel = new CompositeKernel().UseDefaultMagicCommands();
            foreach (var sub in subkernels)
            {
                kernel.Add(sub.LogEventsToPocketLogger());
            }

            kernel.DefaultKernelName = defaultKernelLanguage.LanguageName();

            KernelEvents = kernel.KernelEvents.ToSubscribedList();

            DisposeAfterTest(KernelEvents);
            DisposeAfterTest(kernel);

            return kernel;
        }

        private Kernel CreateFSharpKernel()
        {
            return new FSharpKernel()
                .UseDefaultFormatting()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseDotNetVariableSharing()
                .UseWho()
                .UseDefaultNamespaces();
        }

        private Kernel CreateCSharpKernel()
        {
            return new CSharpKernel()
                .UseDefaultFormatting()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseDotNetVariableSharing()
                .UseWho();
        }

        private Kernel CreatePowerShellKernel()
        {
            return new PowerShellKernel()
                .UseDotNetVariableSharing();
        }

        public async Task SubmitCode(Kernel kernel, string[] submissions, SubmissionType submissionType = SubmissionType.Run)
        {
            foreach (var submission in submissions)
            {
                var cmd = new SubmitCode(submission, submissionType: submissionType);
                await kernel.SendAsync(cmd);
            }
        }

        public async Task SubmitCode(Kernel kernel, string submission, SubmissionType submissionType = SubmissionType.Run)
        {
            var command = new SubmitCode(submission, submissionType: submissionType);
            await kernel.SendAsync(command);
        }

        protected SubscribedList<KernelEvent> KernelEvents { get; private set; }

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
