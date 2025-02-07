// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Pocket.For.Xunit;
using Xunit;
using static Pocket.Logger<Microsoft.DotNet.Interactive.Tests.LanguageKernelTestBase>;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Microsoft.DotNet.Interactive.Tests;

[LogToPocketLogger(FileNameEnvironmentVariable = "POCKETLOGGER_LOG_PATH")]
public abstract class LanguageKernelTestBase : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    static LanguageKernelTestBase()
    {
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error($"{nameof(TaskScheduler.UnobservedTaskException)}", args.Exception);
            args.SetObserved();
        };
    }

    protected LanguageKernelTestBase(ITestOutputHelper output)
    {
        DisposeAfterTest(output.SubscribeToPocketLogger());
    }

    public void Dispose()
    {
        try
        {
            _disposables?.Dispose();
        }
        catch (Exception ex) 
        {
            Log.Error(ex);
        }
    }

    protected virtual CompositeKernel CreateCompositeKernel(Language defaultKernelLanguage = Language.CSharp,
        bool openTestingNamespaces = false)
    {
        return CreateCompositeKernel(
            [
                CreateFSharpKernelAndAliases(openTestingNamespaces),
                CreateCSharpKernelAndAliases(),
                CreatePowerShellKernelAndAliases()
            ],
            defaultKernelLanguage);
    }

    protected CompositeKernel CreateKernel(Language defaultLanguage = Language.CSharp,
        bool openTestingNamespaces = false)
    {
        var languageKernel = defaultLanguage switch
        {
            Language.FSharp => CreateFSharpKernelAndAliases(openTestingNamespaces),
            Language.CSharp => CreateCSharpKernelAndAliases(),
            Language.PowerShell => CreatePowerShellKernelAndAliases(),
            _ => throw new InvalidOperationException($"Unknown language specified: {defaultLanguage}")
        };

        return CreateCompositeKernel([languageKernel], defaultLanguage);
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
            $"""
            open {typeof(Task).Namespace}
            open {typeof(System.Linq.Enumerable).Namespace}
            open {typeof(AspNetCore.Html.IHtmlContent).Namespace}
            open {typeof(FSharp.FSharpKernelHelpers.Html).FullName}
            """;

        kernel.DeferCommand(new SubmitCode(code));
        return kernel;
    }

    private (Kernel, IEnumerable<string>) CreateFSharpKernelAndAliases(bool openTestingNamespaces)
    {
        Kernel kernel =
            new FSharpKernel()
                .UseDefaultFormatting()
                .UseNugetDirective()
                .UseKernelHelpers()
                .UseValueSharing()
                .UseWho();

        if (openTestingNamespaces)
        {
            kernel = UseExtraNamespacesForFSharpTesting(kernel);
        }

        return (kernel, ["f#", "F#"]);
    }

    private (Kernel, IEnumerable<string>) CreateCSharpKernelAndAliases() => 
        (CreateCSharpKernel(), ["c#", "C#"]);

    protected virtual CSharpKernel CreateCSharpKernel() =>
        new CSharpKernel()
            .UseNugetDirective()
            .UseKernelHelpers()
            .UseValueSharing()
            .UseWho();

    private (Kernel, IEnumerable<string>) CreatePowerShellKernelAndAliases() => 
        (new PowerShellKernel().UseValueSharing(), ["powershell"]);

    public async Task SubmitCode(Kernel kernel, string[] submissions)
    {
        foreach (var submission in submissions)
        {
            var cmd = new SubmitCode(submission);
            await kernel.SendAsync(cmd);
        }
    }

    public async Task<KernelCommandResult> SubmitCode(Kernel kernel, string submission)
    {
        var command = new SubmitCode(submission);
        return await kernel.SendAsync(command);
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