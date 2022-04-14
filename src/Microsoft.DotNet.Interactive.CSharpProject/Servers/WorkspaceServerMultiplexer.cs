﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Packaging;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers
{
    public class WorkspaceServerMultiplexer : IWorkspaceServer
    {
        private IPackageFinder _packageFinder;
        private readonly IWorkspaceServer _roslynWorkspaceServer;

        public WorkspaceServerMultiplexer(IPackageFinder packageFinder)
        {
            _packageFinder = packageFinder;
            _roslynWorkspaceServer = new RoslynWorkspaceServer(packageFinder);
        }

        public Task<CompileResult> CompileAsync(WorkspaceRequest request)
        {
            return _roslynWorkspaceServer.CompileAsync(request);
        }

        public Task<CompletionResult> GetCompletionsAsync(WorkspaceRequest request)
        {
            return _roslynWorkspaceServer.GetCompletionsAsync(request);
        }

        public Task<DiagnosticResult> GetDiagnosticsAsync(WorkspaceRequest request)
        {
            return _roslynWorkspaceServer.GetDiagnosticsAsync(request);
        }

        public Task<SignatureHelpResult> GetSignatureHelpAsync(WorkspaceRequest request)
        {
            return _roslynWorkspaceServer.GetSignatureHelpAsync(request);
        }

        public Task<RunResult> RunAsync(WorkspaceRequest request)
        {
            return _roslynWorkspaceServer.RunAsync(request);
        }
    }
}
