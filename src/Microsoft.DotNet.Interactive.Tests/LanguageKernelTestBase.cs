﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Recipes;
using Serilog.Sinks.RollingFileAlternate;
using Xunit.Abstractions;
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

        protected CompositeKernel CreateCompositeKernel(Language defaultKernelLanguage = Language.CSharp,
            bool openTestingNamespaces = false)
        {
            return CreateCompositeKernel(
                new[]
                {
                    CreateFSharpKernel(openTestingNamespaces),
                    CreateCSharpKernel(),
                    CreatePowerShellKernel(),
                },
                defaultKernelLanguage);
        }

        protected CompositeKernel CreateKernel(Language defaultLanguage = Language.CSharp, 
            bool openTestingNamespaces = false)
        {
            var languageKernel = defaultLanguage switch
            {
                Language.FSharp => CreateFSharpKernel(openTestingNamespaces),
                Language.CSharp => CreateCSharpKernel(),
                Language.PowerShell => CreatePowerShellKernel(),
                _ => throw new InvalidOperationException($"Unknown language specified: {defaultLanguage}")
            };

            return CreateCompositeKernel(new[] { languageKernel }, defaultLanguage);
        }

        private CompositeKernel CreateCompositeKernel(IEnumerable<(Kernel, IEnumerable<string>)> subkernelsAndAliases, Language defaultKernelLanguage)
        {
            var kernel = new CompositeKernel().UseDefaultMagicCommands();
            foreach (var (subkernel, aliases) in subkernelsAndAliases)
            {
                kernel.Add(subkernel.LogEventsToPocketLogger(), aliases.ToImmutableArray());
            }

            kernel.DefaultKernelName = defaultKernelLanguage.LanguageName();

            KernelEvents = kernel.KernelEvents.ToSubscribedList();

            DisposeAfterTest(KernelEvents);
            DisposeAfterTest(kernel);

            return kernel;
        }

        private Kernel UseExtraNamespacesForFSharpTesting(Kernel kernel)
        {

            var code =
                 "open " + typeof(System.Threading.Tasks.Task).Namespace + Environment.NewLine +
                 "open " + typeof(System.Linq.Enumerable).Namespace + Environment.NewLine +
                 "open " + typeof(Microsoft.AspNetCore.Html.IHtmlContent).Namespace + Environment.NewLine +
                 "open " + typeof(Microsoft.DotNet.Interactive.FSharp.FSharpKernelHelpers.Html).FullName + Environment.NewLine +
                 "open " + typeof(XPlot.Plotly.PlotlyChart).Namespace + Environment.NewLine;

            kernel.DeferCommand(new SubmitCode(code));
            return kernel;
        }

        private (Kernel, IEnumerable<string>) CreateFSharpKernel(bool openTestingNamespaces)
        {
            Kernel kernel =
                new FSharpKernel()
                .UseDefaultFormatting()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseDotNetVariableSharing()
                .UseWho()
                .UseDefaultNamespaces();

            if (openTestingNamespaces)
            {
                kernel = UseExtraNamespacesForFSharpTesting(kernel);
            }

            return (kernel, new[]
                {
                    "f#",
                    "F#"
                });
        }

        private (Kernel, IEnumerable<string>) CreateCSharpKernel()
        {
            return (new CSharpKernel()
                .UseDefaultFormatting()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseDotNetVariableSharing()
                .UseWho(),
                new[]
                {
                    "c#",
                    "C#"
                });
        }

        private (Kernel, IEnumerable<string>) CreatePowerShellKernel()
        {
            return (new PowerShellKernel()
                .UseDotNetVariableSharing(),
                new[]
                {
                    "powershell"
                });
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
