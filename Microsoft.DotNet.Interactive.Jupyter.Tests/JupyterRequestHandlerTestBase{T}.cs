// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Clockwise;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Tests;
using Pocket;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public abstract class JupyterRequestHandlerTestBase<T> : IDisposable
        where T : Message
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly CSharpKernel _cSharpKernel;
        private readonly FSharpKernel _fSharpKernel;
        private readonly PowerShellKernel _psKernel;
        private readonly CompositeKernel _compositeKernel;
        private readonly JupyterFrontendEnvironment _frontendEnvironment;

        protected RecordingJupyterMessageSender JupyterMessageSender { get; }

        protected IKernel Kernel { get; }

        protected JupyterRequestHandlerTestBase(ITestOutputHelper output)
        {
            _frontendEnvironment = new JupyterFrontendEnvironment();
            _disposables.Add(output.SubscribeToPocketLogger());
            _cSharpKernel = new CSharpKernel()
                .UseDefaultFormatting()
                .UseKernelHelpers()
                .UseJupyterHelpers()
                .UseMathAndLaTeX();

            _fSharpKernel = new FSharpKernel()
                .UseDefaultFormatting()
                .UseKernelHelpers()
                .UseDefaultNamespaces()
                .UseMathAndLaTeX();

            _psKernel = new PowerShellKernel()
                .UseJupyterHelpers();

            _compositeKernel = new CompositeKernel
                {
                    _cSharpKernel,
                    _fSharpKernel,
                    _psKernel
                }
                .UseDefaultMagicCommands();

            _compositeKernel.UseFrontedEnvironment(context => _frontendEnvironment);
            SetKernelLanguage(Language.CSharp);
            _compositeKernel.Name = ".NET";

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

        public void Dispose() => _disposables.Dispose();

        protected ICommandScheduler<JupyterRequestContext> CreateScheduler()
        {
            Shell.SetupDefaultMimeTypes();

            var handler = new JupyterRequestContextHandler(Kernel, _frontendEnvironment);

            return CommandScheduler.Create<JupyterRequestContext>(handler.Handle).Trace();
        }
    }
}