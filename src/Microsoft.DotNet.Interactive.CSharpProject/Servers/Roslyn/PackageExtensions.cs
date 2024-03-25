// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation;
using static System.Environment;
using Package = Microsoft.DotNet.Interactive.CSharpProject.Packaging.Package;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;

internal static class PackageExtensions
{
    public static async Task<Compilation> Compile(
        this Package package, 
        Workspace workspace, 
        BufferId activeBufferId)
    {
        var sourceFiles = workspace.GetSourceFiles().ToArray();

        var (compilation, project) = await package.GetCompilationForRun(sourceFiles, SourceCodeKind.Regular, workspace.Usings);

        var viewports = workspace.ExtractViewPorts();

        var diagnostics = compilation.GetDiagnostics();

        if (workspace.IncludeInstrumentation && !diagnostics.ContainsError())
        {
            var activeDocument = GetActiveDocument(project.Documents, activeBufferId);
            compilation = await AugmentCompilationAsync(viewports, compilation, activeDocument, activeBufferId, package);
        }

        return compilation;
    }

    private static async Task<Compilation> AugmentCompilationAsync(
        IEnumerable<Viewport> viewports,
        Compilation compilation,
        Document document,
        BufferId activeBufferId,
        Package build)
    {
        var regions = InstrumentationLineMapper.FilterActiveViewport(viewports, activeBufferId)
            .Where(v => v.Destination?.Name != null)
            .GroupBy(v => v.Destination.Name,
                v => v.Region,
                (name, region) => new InstrumentationMap(name, region))
            .ToArray();

        var solution = document.Project.Solution;
        var newCompilation = compilation;
        foreach (var tree in newCompilation.SyntaxTrees)
        {
            var replacementRegions = regions.FirstOrDefault(r => tree.FilePath.EndsWith(r.FileToInstrument))?.InstrumentationRegions;

            var subdocument = solution.GetDocument(tree);
            var visitor = new InstrumentationSyntaxVisitor(subdocument, await subdocument.GetSemanticModelAsync(), replacementRegions);
            var linesWithInstrumentation = visitor.Augmentations.Data.Keys;

            var activeViewport = viewports.DefaultIfEmpty(null).First();

            var (augmentationMap, variableLocationMap) =
                await InstrumentationLineMapper.MapLineLocationsRelativeToViewportAsync(
                    visitor.Augmentations,
                    visitor.VariableLocations,
                    document,
                    activeViewport);

            var rewrite = new InstrumentationSyntaxRewriter(
                linesWithInstrumentation,
                variableLocationMap,
                augmentationMap);
            var newRoot = rewrite.Visit(tree.GetRoot());
            var newTree = tree.WithRootAndOptions(newRoot, tree.Options);

            newCompilation = newCompilation.ReplaceSyntaxTree(tree, newTree);
        }

        var instrumentationSyntaxTree = build.GetInstrumentationEmitterSyntaxTree();
        newCompilation = newCompilation.AddSyntaxTrees(instrumentationSyntaxTree);

        var augmentedDiagnostics = newCompilation.GetDiagnostics();
        if (augmentedDiagnostics.ContainsError())
        {
            throw new InvalidOperationException(
                $@"Augmented source failed to compile

Diagnostics
-----------
{string.Join(NewLine, augmentedDiagnostics)}

Source
------
{newCompilation.SyntaxTrees.Select(s => $"// {s.FilePath ?? "(anonymous)"}{NewLine}//---------------------------------{NewLine}{NewLine}{s}").Join(NewLine + NewLine)}");
        }

        return newCompilation;
    }
    
    public static  Task<(Compilation compilation, CodeAnalysis.Project project)> GetCompilationForRun(
        this Package package,
        IReadOnlyCollection<SourceFile> sources,
        SourceCodeKind sourceCodeKind,
        IEnumerable<string> defaultUsings) =>
        package.GetCompilationAsync(sources, sourceCodeKind, defaultUsings, package.CreateWorkspaceForRunAsync);

    public static Task<(Compilation compilation, CodeAnalysis.Project project)> GetCompilationForLanguageServices(
        this Package package,
        IReadOnlyCollection<SourceFile> sources,
        SourceCodeKind sourceCodeKind,
        IEnumerable<string> defaultUsings) =>
        package.GetCompilationAsync(sources, sourceCodeKind, defaultUsings, () => package.CreateWorkspaceAsync());

    private static Document GetActiveDocument(IEnumerable<Document> documents, BufferId activeBufferId)
    {
        return documents.First(d => d.Name.Equals(activeBufferId.FileName));
    }

    public static async Task<(Compilation compilation, CodeAnalysis.Project project)> GetCompilationAsync(
        this Package package,
        IReadOnlyCollection<SourceFile> sources,
        SourceCodeKind sourceCodeKind,
        IEnumerable<string> defaultUsings,
        Func<Task<Microsoft.CodeAnalysis.Workspace>> workspaceFactory)
    {
        var workspace = await workspaceFactory();

        var currentSolution = workspace.CurrentSolution;
        var project = currentSolution.Projects.First();
        var projectId = project.Id;
        foreach (var source in sources)
        {
            if (currentSolution.Projects
                    .SelectMany(p => p.Documents)
                    .FirstOrDefault(d => d.IsMatch(source)) is { } document)
            {
                // there's a pre-existing document, so overwrite its contents
                document = document.WithText(source.Text);
                document = document.WithSourceCodeKind(sourceCodeKind);
                currentSolution = document.Project.Solution;
            }
            else
            {
                var docId = DocumentId.CreateNewId(projectId, $"{package.Name}.Document");

                currentSolution = currentSolution.AddDocument(docId, source.Name, source.Text);
                currentSolution = currentSolution.WithDocumentSourceCodeKind(docId, sourceCodeKind);
            }
        }


        project = currentSolution.GetProject(projectId);
        var usings = defaultUsings?.ToArray() ?? Array.Empty<string>();
        if (usings.Length > 0)
        {
            var options = (CSharpCompilationOptions)project.CompilationOptions;
            project = project.WithCompilationOptions(options.WithUsings(usings));
            currentSolution = project.Solution;
        }

        currentSolution.Workspace.TryApplyChanges(currentSolution);
        currentSolution = workspace.CurrentSolution;
        project = currentSolution.GetProject(projectId);
        var compilation = await project.GetCompilationAsync();
        return (compilation, project);
    }
}