// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests;
using Pocket;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

[Collection("Do not parallelize")]
public abstract class JupyterRequestHandlerTestBase : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly CSharpKernel _cSharpKernel;
    private readonly FSharpKernel _fSharpKernel;
    private readonly PowerShellKernel _psKernel;
    private readonly CompositeKernel _compositeKernel;

    protected RecordingJupyterMessageSender JupyterMessageSender { get; }

    protected Kernel Kernel { get; }

    protected JupyterRequestHandlerTestBase(ITestOutputHelper output)
    {
        _disposables.Add(output.SubscribeToPocketLogger());
        _cSharpKernel = new CSharpKernel()
            .UseNugetDirective()
            .UseKernelHelpers()
            .UseJupyterHelpers();

        _fSharpKernel = new FSharpKernel()
            .UseNugetDirective()
            .UseDefaultFormatting()
            .UseKernelHelpers();

        _psKernel = new PowerShellKernel()
            .UseJupyterHelpers();

        _compositeKernel = new CompositeKernel
            {
                _cSharpKernel,
                _fSharpKernel,
                _psKernel,
                new HtmlKernel()
            }
            .UseDefaultMagicCommands();

        Task.Run(() => JupyterClientKernelExtension.LoadAsync(_compositeKernel)).Wait(5000);

        SetKernelLanguage(Language.CSharp);

        Kernel = _compositeKernel;

        JupyterMessageSender = new RecordingJupyterMessageSender();

        _disposables.Add(_compositeKernel);
        _disposables.Add(Kernel.LogEventsToPocketLogger());
    }

    protected void SetKernelLanguage(Language language)
    {
        switch (language)
        {
            case Language.CSharp:
                _compositeKernel.DefaultKernelName = _cSharpKernel.Name;
                break;
            case Language.FSharp:
                _compositeKernel.DefaultKernelName = _fSharpKernel.Name;
                break;
            case Language.PowerShell:
                _compositeKernel.DefaultKernelName = _psKernel.Name;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(language), language, null);
        }
    }

    protected void DeferCommand(KernelCommand command)
    { 
        _compositeKernel.DeferCommand(command);
    }

    public void Dispose() => _disposables.Dispose();

    protected JupyterRequestContextScheduler CreateScheduler()
    {
        var handler = new JupyterRequestContextHandler(Kernel);
        return new JupyterRequestContextScheduler(handler.Handle);
    }
}