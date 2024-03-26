// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Package = Microsoft.DotNet.Interactive.CSharpProject.Packaging.Package;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;

internal static class PackageExtensions
{
    public static Task<(Compilation compilation, CodeAnalysis.Project project)> GetCompilationForLanguageServices(
        this Package package,
        IReadOnlyCollection<SourceFile> sources,
        SourceCodeKind sourceCodeKind,
        IEnumerable<string> defaultUsings) =>
        package.GetCompilationAsync(sources, sourceCodeKind, defaultUsings, package.GetOrCreateWorkspaceAsync);

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