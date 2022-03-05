// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Pocket;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests
{
    public abstract class WorkspaceServerTestsCore : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        protected WorkspaceServerTestsCore(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        public void Dispose() => _disposables.Dispose();

        protected abstract ILanguageService GetLanguageService();

        protected abstract ICodeCompiler GetCodeCompiler();

        protected abstract ICodeRunner GetCodeRunner();
    }
}