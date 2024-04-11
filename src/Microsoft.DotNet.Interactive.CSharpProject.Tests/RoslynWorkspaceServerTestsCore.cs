// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.CSharpProject.Build;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public abstract class RoslynWorkspaceServerTestsCore : WorkspaceServerTestsCore
{
    protected RoslynWorkspaceServerTestsCore(ITestOutputHelper output) : base(output)
    {
    }

    protected override ILanguageService GetLanguageService() => new RoslynWorkspaceServer(PrebuildFinder.Create(() => Prebuild.GetOrCreateConsolePrebuildAsync(false)));

    protected override ICodeCompiler GetCodeCompiler() => new RoslynWorkspaceServer(PrebuildFinder.Create(() => Prebuild.GetOrCreateConsolePrebuildAsync(false)));

    protected override ICodeRunner GetCodeRunner() => new RoslynWorkspaceServer(PrebuildFinder.Create(() => Prebuild.GetOrCreateConsolePrebuildAsync(false)));
}