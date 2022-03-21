// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharpProject.Commands;
using Microsoft.DotNet.Interactive.CSharpProject.Events;
using Microsoft.DotNet.Interactive.CSharpProject.MLS.Project;
using Microsoft.DotNet.Interactive.CSharpProject.Packaging;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

namespace Microsoft.DotNet.Interactive.CSharpProject
{
    public class CSharpProjectKernel
        : Kernel
    //IKernelCommandHandler<OpenProject>,
    //IKernelCommandHandler<OpenDocument>,
    //IKernelCommandHandler<CompileProject>,
    //IKernelCommandHandler<RequestCompletions>,
    //IKernelCommandHandler<RequestDiagnostics>,
    //IKernelCommandHandler<RequestSignatureHelp>
    {
        private RoslynWorkspaceServer _workspaceServer;
        private Protocol.Workspace _workspace;
        private Protocol.Buffer _buffer;

        static CSharpProjectKernel()
        {
            // register commands and event with serialization
            KernelCommandEnvelope.RegisterCommand<OpenProject>();
            KernelCommandEnvelope.RegisterCommand<OpenDocument>();
            KernelCommandEnvelope.RegisterCommand<CompileProject>();

            KernelEventEnvelope.RegisterEvent<DocumentOpened>();
            KernelEventEnvelope.RegisterEvent<AssemblyProduced>();
        }

        public CSharpProjectKernel(string name)
            : base(name)
        {
            RegisterCommandHandler<OpenProject>(HandleAsync);
            RegisterCommandHandler<OpenDocument>(HandleAsync);
            RegisterCommandHandler<CompileProject>(HandleAsync);
            RegisterCommandHandler<RequestCompletions>(HandleAsync);
            RegisterCommandHandler<RequestDiagnostics>(HandleAsync);
            RegisterCommandHandler<RequestSignatureHelp>(HandleAsync);
        }

        public async Task HandleAsync(OpenProject command, KernelInvocationContext context)
        {
            var package = await CreateConsoleWorkspacePackage();
            _workspaceServer = new RoslynWorkspaceServer(package);

            _workspace = new Protocol.Workspace(
                files: command.Project.Files.Select(f => new Protocol.File(f.RelativePath, f.Content)).ToArray());
        }

        public async Task HandleAsync(OpenDocument command, KernelInvocationContext context)
        {
            EnsureProjectOpened();

            var file = _workspace.Files.SingleOrDefault(f => f.Name == command.RelativePath);
            if (file is null)
            {
                // add it to the workspace
                file = new Protocol.File(command.RelativePath, string.Empty);
                _workspace = new Protocol.Workspace(
                    files: _workspace.Files.Concat(new[] { file }).ToArray());
            }

            if (string.IsNullOrWhiteSpace(command.RegionName))
            {
                _buffer = new Protocol.Buffer(file.Name, file.Text);
            }
            else
            {
                var extractor = new BufferFromRegionExtractor();
                _workspace = extractor.Extract(_workspace.Files);
                _buffer = _workspace.Buffers.SingleOrDefault(b => b.Id.FileName == command.RelativePath && b.Id.RegionName == command.RegionName);
                if (_buffer is null)
                {
                    throw new Exception($"Region '{command.RegionName}' not found in file '{command.RelativePath}'");
                }
            }

            context.Publish(new DocumentOpened(command, command.RelativePath, command.RegionName, _buffer.Content));
        }

        public async Task HandleAsync(CompileProject command, KernelInvocationContext context)
        {
            EnsureProjectOpened();
            EnsureDocumentOpened();

            var request = await GetWorkspaceRequestWithCode(command.Code);
            var result = await _workspaceServer.Compile(request);

            var diagnostics = GetDiagnostics(command.Code, result);
            context.Publish(new DiagnosticsProduced(diagnostics, command));
            if (diagnostics.Any(d => d.Severity == CodeAnalysis.DiagnosticSeverity.Error))
            {
                context.Fail(command);
                return;
            }

            context.Publish(new AssemblyProduced(command, new Base64EncodedAssembly(result.Base64Assembly)));
        }

        public async Task HandleAsync(RequestCompletions command, KernelInvocationContext context)
        {
            EnsureProjectOpened();
            EnsureDocumentOpened();

            var position = GetPositionFromLinePosition(command.Code, command.LinePosition);
            var request = await GetWorkspaceRequestWithCode(command.Code, position);
            var completionResult = await _workspaceServer.GetCompletionList(request, new Budget());
            var completionItems = completionResult.Items.Select(item => new CompletionItem(item.DisplayText, item.Kind, item.FilterText, item.SortText, item.InsertText, item.Documentation?.Value)).ToList();

            context.Publish(new CompletionsProduced(completionItems, command));
        }

        public async Task HandleAsync(RequestDiagnostics command, KernelInvocationContext context)
        {
            EnsureProjectOpened();
            EnsureDocumentOpened();

            var request = await GetWorkspaceRequestWithCode(command.Code);
            var result = await _workspaceServer.Compile(request);

            var diagnostics = GetDiagnostics(command.Code, result);
            context.Publish(new DiagnosticsProduced(diagnostics, command));
        }

        public async Task HandleAsync(RequestSignatureHelp command, KernelInvocationContext context)
        {
            EnsureProjectOpened();
            EnsureDocumentOpened();

            var position = GetPositionFromLinePosition(command.Code, command.LinePosition);
            var request = await GetWorkspaceRequestWithCode(command.Code, position);
            var sigHelpResult = await _workspaceServer.GetSignatureHelp(request, new Budget());
            var sigHelpItems = sigHelpResult.Signatures.Select(s =>
                new SignatureInformation(
                    s.Label,
                    new FormattedValue("text/markdown", s.Documentation.Value),
                    s.Parameters.Select(p => new ParameterInformation(p.Label, new FormattedValue("text/markdown", p.Documentation.Value))).ToList())).ToList();

            context.Publish(new SignatureHelpProduced(command, sigHelpItems, sigHelpResult.ActiveSignature, sigHelpResult.ActiveParameter));
        }

        private static int GetPositionFromLinePosition(string code, LinePosition linePosition)
        {
            var position = 0;
            var currentLine = 0;
            var currentCharacter = 0;
            foreach (var c in code)
            {
                if (currentLine == linePosition.Line &&
                    currentCharacter == linePosition.Character)
                {
                    return position;
                }

                position++;
                currentCharacter++;
                if (c == '\n')
                {
                    currentLine++;
                    currentCharacter = 0;
                }
            }

            return position;
        }

        private static LinePosition GetLinePositionFromPosition(string code, int position)
        {
            var currentPosition = 0;
            var currentLine = 0;
            var currentCharacter = 0;
            foreach (var c in code)
            {
                if (currentPosition == position)
                {
                    return new LinePosition(currentLine, currentCharacter);
                }

                currentPosition++;
                currentCharacter++;
                if (c == '\n')
                {
                    currentLine++;
                    currentCharacter = 0;
                }
            }

            return new LinePosition(currentLine, currentCharacter);
        }

        private async Task<Protocol.WorkspaceRequest> GetWorkspaceRequestWithCode(string code, int position = 0)
        {
            var updatedWorkspace = new Protocol.Workspace(
                files: _workspace.Files,
                buffers: _workspace.Buffers.Where(b => b.Id != _buffer.Id).Concat(new[] { new Protocol.Buffer(_buffer.Id, code, position: position) }).ToArray());
            var inlinedWorkspace = await updatedWorkspace.InlineBuffersAsync();

            var workspaceRequest = new Protocol.WorkspaceRequest(inlinedWorkspace, _buffer.Id);
            return workspaceRequest;
        }

        private static IEnumerable<Diagnostic> GetDiagnostics(string code, Protocol.CompileResult result)
        {
            var diagnostics = Enumerable.Empty<Protocol.SerializableDiagnostic>();
            var projectDiagnostics = Enumerable.Empty<Protocol.SerializableDiagnostic>();

            if (result.Features.TryGetValue(nameof(Protocol.Diagnostics), out var candidateDiagnostics) &&
                candidateDiagnostics is Protocol.Diagnostics diags)
            {
                diagnostics = diags;
            }

            if (result.Features.TryGetValue(nameof(Protocol.ProjectDiagnostics), out var candidateProjectDiagnostics) &&
                candidateDiagnostics is Protocol.ProjectDiagnostics projectDiags)
            {
                projectDiagnostics = projectDiags;
            }

            var allDiagnostics = diagnostics.Concat(projectDiagnostics);

            var finalDiagnostics = diagnostics.Select(d => new Diagnostic(new LinePositionSpan(GetLinePositionFromPosition(code, d.Start), GetLinePositionFromPosition(code, d.End)), ConvertSeverity(d.Severity), d.Id, d.Message));

            return finalDiagnostics;
        }

        private static CodeAnalysis.DiagnosticSeverity ConvertSeverity(Protocol.DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case Protocol.DiagnosticSeverity.Hidden:
                    return CodeAnalysis.DiagnosticSeverity.Hidden;
                case Protocol.DiagnosticSeverity.Info:
                    return CodeAnalysis.DiagnosticSeverity.Info;
                case Protocol.DiagnosticSeverity.Warning:
                    return CodeAnalysis.DiagnosticSeverity.Warning;
                case Protocol.DiagnosticSeverity.Error:
                    return CodeAnalysis.DiagnosticSeverity.Error;
                default:
                    return CodeAnalysis.DiagnosticSeverity.Warning;
            }
        }

        private void EnsureProjectOpened()
        {
            if (_workspaceServer is null || _workspace is null)
            {
                throw new Exception($"Project must be opened, send the command '{nameof(OpenProject)}' first.");
            }
        }

        private void EnsureDocumentOpened()
        {
            if (_buffer is null)
            {
                throw new Exception($"Document must be opened, send the command '{nameof(OpenDocument)}' first.");
            }
        }

        private static async Task<Package> CreateConsoleWorkspacePackage()
        {
            var packageBuilder = new PackageBuilder("console");
            packageBuilder.CreateUsingDotnet("console");
            packageBuilder.TrySetLanguageVersion("8.0");
            packageBuilder.AddPackageReference("Newtonsoft.Json", "13.0.1");
            var package = packageBuilder.GetPackage() as Package;
            await package.CreateRoslynWorkspaceForRunAsync(new Budget());
            return package;
        }
    }
}
