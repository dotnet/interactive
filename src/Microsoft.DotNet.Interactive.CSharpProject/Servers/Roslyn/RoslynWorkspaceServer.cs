// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.DotNet.Interactive.Utility;
using Recipes;
using Microsoft.DotNet.Interactive.CSharpProject.Models.Execution;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation;
using Microsoft.DotNet.Interactive.CSharpProject.Transformations;
using Microsoft.DotNet.Interactive.CSharpProject.LanguageServices;
using Microsoft.DotNet.Interactive.CSharpProject.Packaging;
using Pocket;
using static Pocket.Logger;
using Package = Microsoft.DotNet.Interactive.CSharpProject.Packaging.Package;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;

public class RoslynWorkspaceServer : IWorkspaceServer
{
    private readonly IPackageFinder _packageFinder;

    private static readonly ConcurrentDictionary<string, AsyncLock> locks = new();

    static RoslynWorkspaceServer()
    {
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Warning($"{nameof(TaskScheduler.UnobservedTaskException)}", args.Exception);
            args.SetObserved();
        };
    }

    public RoslynWorkspaceServer(Func<Task<Package>> package)
    {
        _packageFinder = PackageFinder.Create(package);
    }

    public RoslynWorkspaceServer(IPackageFinder packageRegistry)
    {
        _packageFinder = packageRegistry ?? throw new ArgumentNullException(nameof(packageRegistry));
    }

    public async Task<CompletionResult> GetCompletionsAsync(WorkspaceRequest request)
    {
        var package = await _packageFinder.FindAsync(request.Workspace.WorkspaceType);

        var workspace = await request.Workspace.InlineBuffersAsync();
        var sourceFiles = workspace.GetSourceFiles();

        // get project and ensure the solution is up-to-date
        var (_, project) = await package.GetCompilationForLanguageServices(
            sourceFiles,
            GetSourceCodeKind(request),
            GetUsings(request.Workspace));
        var documents = project.Documents.ToList();
        var solution = project.Solution;

        // get most up-to-date document
        var file = workspace.GetContentFromBufferId(request.ActiveBufferId);
        var selectedDocumentId = documents.First(doc => doc.IsMatch(file)).Id;
        var selectedDocument = solution.GetDocument(selectedDocumentId);

        var service = CompletionService.GetService(selectedDocument);

        var (_, _, absolutePosition) = workspace.GetTextLocation(request.ActiveBufferId);
        var semanticModel = await selectedDocument!.GetSemanticModelAsync();
        var diagnostics = DiagnosticsExtractor.ExtractSerializableDiagnosticsFromSemanticModel(request.ActiveBufferId, semanticModel, workspace);

        var symbols = await Recommender.GetRecommendedSymbolsAtPositionAsync(
            selectedDocument,
            absolutePosition,
            solution.Workspace.Options);
            
        var symbolToSymbolKey = new Dictionary<(string, int), ISymbol>();
            
        foreach (var symbol in symbols)
        {
            var key = (symbol.Name, (int)symbol.Kind);
            if (!symbolToSymbolKey.ContainsKey(key))
            {
                symbolToSymbolKey[key] = symbol;
            }
        }

        if (service is null)
        {
            return new CompletionResult(requestId: request.RequestId, diagnostics: diagnostics);
        }
            
        var completionList = await service.GetCompletionsAsync(selectedDocument!, absolutePosition);

        var completionItems = completionList.ItemsList
            .Select(item => item.ToModel(symbolToSymbolKey, selectedDocument));

        return new CompletionResult(completionItems
                .Deduplicate()
                .ToArray(),
            requestId: request.RequestId,
            diagnostics: diagnostics);

    }

    private SourceCodeKind GetSourceCodeKind(WorkspaceRequest request)
    {
        return request.Workspace.WorkspaceType == "script"
            ? SourceCodeKind.Script
            : SourceCodeKind.Regular;
    }

    private IEnumerable<string> GetUsings(Workspace workspace)
    {
        return workspace.WorkspaceType == "script"
            ? workspace.Usings.Concat(WorkspaceUtilities.DefaultUsingDirectives).Distinct()
            : workspace.Usings;
    }

    public async Task<SignatureHelpResult> GetSignatureHelpAsync(WorkspaceRequest request)
    {
        var package = await _packageFinder.FindAsync(request.Workspace.WorkspaceType);

        var workspace = await request.Workspace.InlineBuffersAsync();

        var sourceFiles = workspace.GetSourceFiles();
        var (compilation, project) = await package.GetCompilationForLanguageServices(sourceFiles, GetSourceCodeKind(request), GetUsings(request.Workspace));
        var documents = project.Documents.ToList();

        var selectedDocument = documents.FirstOrDefault(doc => doc.IsMatch(request.ActiveBufferId.FileName))
                               ??
                               (documents.Count == 1 ? documents.Single() : null);

        if (selectedDocument == null)
        {
            return new SignatureHelpResult(requestId: request.RequestId);
        }

        var diagnostics = await DiagnosticsExtractor.ExtractSerializableDiagnosticsFromDocument(request.ActiveBufferId, selectedDocument, workspace);

        var tree = await selectedDocument.GetSyntaxTreeAsync();

        var absolutePosition = workspace.GetAbsolutePositionForBufferByIdOrSingleBufferIfThereIsOnlyOne(request.ActiveBufferId);

        var syntaxNode = tree.GetRoot().FindToken(absolutePosition).Parent;

        var result = await SignatureHelpService.GetSignatureHelpAsync(
            () => Task.FromResult(compilation.GetSemanticModel(tree)),
            syntaxNode,
            absolutePosition);
        result.RequestId = request.RequestId;
        if (diagnostics?.Count > 0)
        {
            result.Diagnostics = diagnostics;
        }

        return result;
    }

    public async Task<DiagnosticResult> GetDiagnosticsAsync(WorkspaceRequest request)
    {
        var package = await _packageFinder.FindAsync(request.Workspace.WorkspaceType);

        var workspace = await request.Workspace.InlineBuffersAsync();

        var sourceFiles = workspace.GetSourceFiles();
        var (_, project) = await package.GetCompilationForLanguageServices(sourceFiles, GetSourceCodeKind(request), GetUsings(request.Workspace));
        var documents = project.Documents.ToList();

        var selectedDocument = documents.FirstOrDefault(doc => doc.IsMatch( request.ActiveBufferId.FileName))
                               ??
                               (documents.Count == 1 ? documents.Single() : null);

        if (selectedDocument == null)
        {
            return new DiagnosticResult(requestId: request.RequestId);
        }

        var diagnostics = await DiagnosticsExtractor.ExtractSerializableDiagnosticsFromDocument(request.ActiveBufferId, selectedDocument, workspace);

        var result = new DiagnosticResult(diagnostics, request.RequestId);
        return result;
    }

    public async Task<CompileResult> CompileAsync(WorkspaceRequest request)
    {
        var workspace = request.Workspace;

        using (await locks.GetOrAdd(workspace.WorkspaceType, _ => new AsyncLock()).LockAsync())
        {
            var result = await GetCompilationAsync(request.Workspace, request.ActiveBufferId);

            if (result.DiagnosticsWithinBuffers.ContainsError())
            {
                var compileResult = new CompileResult(
                    succeeded: false,
                    base64assembly: null,
                    result.DiagnosticsWithinBuffers,
                    requestId: request.RequestId);

                compileResult.AddFeature(new ProjectDiagnostics(result.ProjectDiagnostics));

                return compileResult;
            }

            using (var stream = new MemoryStream())
            {
                result.Compilation.Emit(peStream: stream);
                var encodedAssembly = Convert.ToBase64String(stream.ToArray());

                var compileResult = new CompileResult(
                    succeeded: true,
                    base64assembly: encodedAssembly,
                    requestId: request.RequestId);

                compileResult.AddFeature(new ProjectDiagnostics(result.ProjectDiagnostics));
                  
                return compileResult;
            }
        }
    }

    public async Task<RunResult> RunAsync(WorkspaceRequest request)
    {
        var workspace = request.Workspace;

        using (await locks.GetOrAdd(workspace.WorkspaceType, s => new AsyncLock()).LockAsync())
        {
            var package = await _packageFinder.FindAsync(workspace.WorkspaceType);

            var result = await GetCompilationAsync(request.Workspace, request.ActiveBufferId);

            if (result.ProjectDiagnostics.ContainsError())
            {
                var errorMessagesToDisplayInOutput = result.DiagnosticsWithinBuffers.Any()
                    ? result.DiagnosticsWithinBuffers.GetCompileErrorMessages()
                    : result.ProjectDiagnostics.GetCompileErrorMessages();

                var runResult = new RunResult(
                    false,
                    errorMessagesToDisplayInOutput,
                    diagnostics: result.DiagnosticsWithinBuffers,
                    requestId: request.RequestId);

                runResult.AddFeature(new ProjectDiagnostics(result.ProjectDiagnostics));

                return runResult;
            }

            await EmitCompilationAsync(result.Compilation, package);

            return await RunConsoleAsync(
                package,
                result.DiagnosticsWithinBuffers,
                request.RequestId,
                workspace.IncludeInstrumentation,
                request.RunArgs);
        }
    }

    private static async Task EmitCompilationAsync(Compilation compilation, Package package)
    {
        if (package is null)
        {
            throw new ArgumentNullException(nameof(package));
        }

        var numberOfAttempts = 100;
        for (var attempt = 1; attempt < numberOfAttempts; attempt++)
        {
            try
            {
                compilation.Emit(package.EntryPointAssemblyPath.FullName);
                break;
            }
            catch (IOException)
            {
                if (attempt == numberOfAttempts - 1)
                {
                    throw;
                }

                await Task.Delay(10);
            }
        }
    }

    internal static async Task<RunResult> RunConsoleAsync(
        Package package,
        IEnumerable<SerializableDiagnostic> diagnostics,
        string requestId,
        bool includeInstrumentation,
        string commandLineArgs)
    {
        var dotnet = new Dotnet(package.Directory);

        var commandName = $@"""{package.EntryPointAssemblyPath.FullName}""";
        var commandLineResult = await dotnet.Execute(
            commandName.AppendArgs(commandLineArgs));

        var output = InstrumentedOutputExtractor.ExtractOutput(commandLineResult.Output);

        if (commandLineResult.ExitCode == 124)
        {
            throw new TimeoutException();
        }

        string exceptionMessage = null;

        if (commandLineResult.Error.Count > 0)
        {
            exceptionMessage = string.Join(Environment.NewLine, commandLineResult.Error);
        }

        var runResult = new RunResult(
            succeeded: true,
            output: output.StdOut,
            exception: exceptionMessage,
            diagnostics: diagnostics,
            requestId: requestId);

        if (includeInstrumentation)
        {
            runResult.AddFeature(output.ProgramStatesArray);
            runResult.AddFeature(output.ProgramDescriptor);
        }

        return runResult;
    }
    
    private async Task<CompilationResult> GetCompilationAsync(
        Workspace workspace,
        BufferId activeBufferId)
    {
        var package = await _packageFinder.FindAsync(workspace.WorkspaceType);
        workspace = await workspace.InlineBuffersAsync();
        var sources = workspace.GetSourceFiles();
        var (compilation, project) = await package.GetCompilationAsync(sources, SourceCodeKind.Regular, workspace.Usings, () => package.CreateWorkspaceAsync());
        var (diagnosticsInActiveBuffer, allDiagnostics) = workspace.MapDiagnostics(activeBufferId, compilation.GetDiagnostics());
        return new CompilationResult(
            compilation,
            diagnosticsInActiveBuffer,
            allDiagnostics);
    }

    private class CompilationResult
    {
        public CompilationResult(
            Compilation compilation,
            IReadOnlyCollection<SerializableDiagnostic> diagnosticsInActiveBuffer,
            IReadOnlyCollection<SerializableDiagnostic> allDiagnostics)
        {
            Compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
            DiagnosticsWithinBuffers = diagnosticsInActiveBuffer ?? throw new ArgumentNullException(nameof(diagnosticsInActiveBuffer));
            ProjectDiagnostics = allDiagnostics ?? throw new ArgumentNullException(nameof(allDiagnostics));
        }

        public Compilation Compilation { get; }
        public IReadOnlyCollection<SerializableDiagnostic> DiagnosticsWithinBuffers { get; }
        public IReadOnlyCollection<SerializableDiagnostic> ProjectDiagnostics { get; }
    }
}