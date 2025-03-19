// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Build;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;
using Pocket;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public abstract class WorkspaceServerTestsCore : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public void Dispose() => _disposables.Dispose();

    protected ILanguageService GetLanguageService() => CreateRoslynWorkspaceServer();

    protected ICodeCompiler GetCodeCompiler() => CreateRoslynWorkspaceServer();

    protected ICodeRunner GetCodeRunner() => CreateRoslynWorkspaceServer();

    [ClassInitialize(InheritanceBehavior.BeforeEachDerivedClass)]
    public static async Task ClassInitialize(TestContext testContext)
    {
        await PrebuildFixture.Instance.InitializeAsync();
    }

    private WorkspaceServer CreateRoslynWorkspaceServer()
    {
        return new WorkspaceServer(PrebuildFinder.Create(() => Task.FromResult(PrebuildFixture.Instance.Prebuild)));
    }
}