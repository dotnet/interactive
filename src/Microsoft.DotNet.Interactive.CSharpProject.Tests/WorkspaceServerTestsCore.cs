// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Build;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;
using Pocket;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

[Collection(nameof(PrebuildFixture))]
public abstract class WorkspaceServerTestsCore : IDisposable
{
    private readonly PrebuildFixture _prebuildFixture;
    private readonly CompositeDisposable _disposables = new();

    protected WorkspaceServerTestsCore(
        PrebuildFixture prebuildFixture, 
        ITestOutputHelper output)
    {
        _prebuildFixture = prebuildFixture;
    }

    public void Dispose() => _disposables.Dispose();

    protected ILanguageService GetLanguageService() => CreateRoslynWorkspaceServer();

    protected ICodeCompiler GetCodeCompiler() => CreateRoslynWorkspaceServer();

    protected ICodeRunner GetCodeRunner() => CreateRoslynWorkspaceServer();

    private WorkspaceServer CreateRoslynWorkspaceServer()
    {
        return new WorkspaceServer(PrebuildFinder.Create(() => Task.FromResult(_prebuildFixture.Prebuild)));
    }
}