// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class CSharpProjectKernel : Kernel
    {
        private RoslynWorkspaceServer _workspaceServer;
        private Workspace _workspace;
        private Buffer _buffer;

        public static void RegisterEventsAndCommands()
        {
            // register commands and event with serialization

            var commandTypes = typeof(CSharpProjectKernel).Assembly.ExportedTypes
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => typeof(KernelCommand).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .ToList();
            var eventTypes = typeof(CSharpProjectKernel).Assembly.ExportedTypes
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => typeof(KernelEvent).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .ToList();

            foreach (var commandType in commandTypes)
            {
                KernelCommandEnvelope.RegisterCommand(commandType);
            }

            foreach (var eventType in eventTypes)
            {
                KernelEventEnvelope.RegisterEvent(eventType);
            }
        }

        public CSharpProjectKernel(string name)
            : base(name, "C#")
        {
            RegisterCommandHandler<OpenProject>(HandleAsync);
            RegisterCommandHandler<OpenDocument>(HandleAsync);
            RegisterCommandHandler<CompileProject>(HandleAsync);
            RegisterCommandHandler<RequestCompletions>(HandleAsync);
            RegisterCommandHandler<RequestDiagnostics>(HandleAsync);
            RegisterCommandHandler<RequestSignatureHelp>(HandleAsync);
            RegisterCommandHandler<SubmitCode>(HandleAsync);
        }

        public async Task HandleAsync(OpenProject command, KernelInvocationContext context)
        {
            var package = await CreateConsoleWorkspacePackage();
            _workspaceServer = new RoslynWorkspaceServer(package);

            var extractor = new BufferFromRegionExtractor();
            _workspace = extractor.Extract(command.Project.Files.Select(f => new ProjectFileContent(f.RelativeFilePath, f.Content)).ToArray());

            context.Publish(new ProjectOpened(command, _workspace.Buffers.GroupBy(b => b.Id.FileName)
                .OrderBy(g => g.Key).Select(g => new ProjectItem(g.Key, g.Select(r => r.Id.RegionName).Where(r => r != null).OrderBy(r => r).ToList())).ToList()));
        }

        public async Task HandleAsync(OpenDocument command, KernelInvocationContext context)
        {
            ThrowIfProjectIsNotOpened();

            var file = _workspace.Files.SingleOrDefault(f => f.Name == command.RelativeFilePath);
            if (file is null)
            {
                // check for a region-less buffer instead
                var buffer = _workspace.Buffers.SingleOrDefault(b => b.Id.FileName == command.RelativeFilePath && b.Id.RegionName is null);
                if (buffer is { })
                {
                    // create a temporary file with the buffer's content
                    file = new(command.RelativeFilePath, buffer.Content);
                }
                else
                {
                    // add it to the workspace
                    file = new(command.RelativeFilePath, string.Empty);
                    _workspace = new Workspace(
                        files: _workspace.Files.Concat(new[] { file }).ToArray());
                }
            }

            if (string.IsNullOrWhiteSpace(command.RegionName))
            {
                _buffer = new Buffer(file.Name, file.Text);
            }
            else
            {
                var extractor = new BufferFromRegionExtractor();
                _workspace = extractor.Extract(_workspace.Files);
                _buffer = _workspace.Buffers.SingleOrDefault(b => b.Id.FileName == command.RelativeFilePath && b.Id.RegionName == command.RegionName);
                if (_buffer is null)
                {
                    throw new Exception($"Region '{command.RegionName}' not found in file '{command.RelativeFilePath}'");
                }
            }

            context.Publish(new DocumentOpened(command, command.RelativeFilePath, command.RegionName, _buffer.Content));
        }

        public async Task HandleAsync(SubmitCode command, KernelInvocationContext context)
        {
            ThrowIfProjectIsNotOpened();
            ThrowIfDocumentIsNotOpened();

            var updatedWorkspace = await GetWorkspaceWithCode(command.Code);
            _buffer = updatedWorkspace.Buffers.Single(b => b.Id == _buffer.Id);
            _workspace = updatedWorkspace;
        }

        public async Task HandleAsync(CompileProject command, KernelInvocationContext context)
        {
            ThrowIfProjectIsNotOpened();
            ThrowIfDocumentIsNotOpened();

            var request = new WorkspaceRequest(_workspace, _buffer.Id);
            var result = await _workspaceServer.CompileAsync(request);

            var diagnostics = GetDiagnostics(_buffer.Content, result);
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
            ThrowIfProjectIsNotOpened();
            ThrowIfDocumentIsNotOpened();

            var position = GetPositionFromLinePosition(command.Code, command.LinePosition);
            var updatedWorkspace = await GetWorkspaceWithCode(command.Code, position);
            var request = new WorkspaceRequest(updatedWorkspace, _buffer.Id);
            var completionResult = await _workspaceServer.GetCompletionsAsync(request);
            var completionItems = completionResult.Items.Select(item => new CompletionItem(
                displayText: item.DisplayText,
                kind: item.Kind,
                filterText: item.FilterText,
                sortText: item.SortText,
                insertText: item.InsertText,
                documentation: item.Documentation)).ToList();

            context.Publish(new CompletionsProduced(completionItems, command));
        }

        public async Task HandleAsync(RequestDiagnostics command, KernelInvocationContext context)
        {
            ThrowIfProjectIsNotOpened();
            ThrowIfDocumentIsNotOpened();

            var updatedWorkspace = await GetWorkspaceWithCode(command.Code);
            var request = new WorkspaceRequest(updatedWorkspace, _buffer.Id);
            var result = await _workspaceServer.CompileAsync(request);

            var diagnostics = GetDiagnostics(command.Code, result);
            context.Publish(new DiagnosticsProduced(diagnostics, command));
        }

        public async Task HandleAsync(RequestSignatureHelp command, KernelInvocationContext context)
        {
            ThrowIfProjectIsNotOpened();
            ThrowIfDocumentIsNotOpened();

            var position = GetPositionFromLinePosition(command.Code, command.LinePosition);
            var updatedWorkspace = await GetWorkspaceWithCode(command.Code, position);
            var request = new WorkspaceRequest(updatedWorkspace, _buffer.Id);
            var sigHelpResult = await _workspaceServer.GetSignatureHelpAsync(request);
            var sigHelpItems = sigHelpResult.Signatures.Select(s =>
                new SignatureInformation(
                    s.Label,
                    s.Documentation,
                    s.Parameters.Select(p => new ParameterInformation(p.Label, p.Documentation)).ToArray())).ToArray();

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

        private async Task<Workspace> GetWorkspaceWithCode(string code, int position = 0)
        {
            var updatedWorkspace = new Workspace(
                files: _workspace.Files,
                buffers: _workspace.Buffers.Where(b => b.Id != _buffer.Id).Concat(new[] { new Buffer(_buffer.Id, code, position: position) }).ToArray());
            var inlinedWorkspace = await updatedWorkspace.InlineBuffersAsync();
            return inlinedWorkspace;
        }

        private static IEnumerable<Diagnostic> GetDiagnostics(string code, CompileResult result)
        {
            var diagnostics = Enumerable.Empty<SerializableDiagnostic>();
            var projectDiagnostics = Enumerable.Empty<SerializableDiagnostic>();

            if (result.Features.TryGetValue(nameof(Diagnostics), out var candidateDiagnostics) &&
                candidateDiagnostics is Diagnostics diags)
            {
                diagnostics = diags;
            }

            if (result.Features.TryGetValue(nameof(ProjectDiagnostics), out var candidateProjectDiagnostics) &&
                candidateDiagnostics is ProjectDiagnostics projectDiags)
            {
                projectDiagnostics = projectDiags;
            }

            var allDiagnostics = diagnostics.Concat(projectDiagnostics);

            var finalDiagnostics = diagnostics.Select(d => new Diagnostic(new LinePositionSpan(GetLinePositionFromPosition(code, d.Start), GetLinePositionFromPosition(code, d.End)), d.Severity, d.Id, d.Message));

            return finalDiagnostics;
        }
        
        private void ThrowIfProjectIsNotOpened()
        {
            if (_workspaceServer is null || _workspace is null)
            {
                throw new InvalidOperationException($"Project must be opened, send the command '{nameof(OpenProject)}' first.");
            }
        }

        private void ThrowIfDocumentIsNotOpened()
        {
            if (_buffer is null)
            {
                throw new InvalidOperationException($"Document must be opened, send the command '{nameof(OpenDocument)}' first.");
            }
        }

        private static async Task<Package> CreateConsoleWorkspacePackage()
        {
            var packageBuilder = new PackageBuilder("console");
            packageBuilder.CreateUsingDotnet("console");
            packageBuilder.TrySetLanguageVersion("8.0");
            packageBuilder.AddPackageReference("Newtonsoft.Json", "13.0.1");
            var package = packageBuilder.GetPackage() as Package;
            await package!.CreateWorkspaceForRunAsync();
            return package;
        }
    }
}
