// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.CSharp;

internal class InteractiveWorkspace : Workspace
{
    private ProjectId _previousSubmissionProjectId;
    private ProjectId _workingProjectId;
    private readonly CompositeDisposable _disposables = new();
    private int _submissionCount;
    private readonly CSharpParseOptions _parseOptions;
    private Compilation _currentCompilation;
    private DocumentId _workingDocumentId;
    private readonly IReadOnlyCollection<MetadataReference> _referenceAssemblies;
    private readonly List<MetadataReference> _packageManagerReferences = new();
    private readonly SemaphoreSlim _workspaceLock = new(1, 1);

    public InteractiveWorkspace() : base(MefHostServices.DefaultHost, WorkspaceKind.Interactive)
    {
        _parseOptions = new CSharpParseOptions(
            LanguageVersion.Latest,
            DocumentationMode.Parse,
            SourceCodeKind.Script);

        _referenceAssemblies = ResolveRefAssemblies();

        _disposables.Add(Disposable.Create(() =>
        {
            _currentCompilation = null;
        }));
    }

    private static string ResolveRefAssemblyPath()
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location);
        var refAssemblyDir = runtimeDir; // if any of the below path probing fails, fall back to the runtime so we can still run

        // e.g., will transform
        //   `C:\Program Files\dotnet\shared\Microsoft.NETCore.App\5.0.3`
        // to
        //   `C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\5.0.0\ref\net5.0`
        if (TryParseVersion(Path.GetFileName(runtimeDir), out var runtimeVersion))
        {
            var appRefDir = Path.Combine(runtimeDir, "..", "..", "..", "packs", "Microsoft.NETCore.App.Ref");
            if (Directory.Exists(appRefDir))
            {
                var latestRuntimeDirAndVersion =
                    Directory
                        .GetDirectories(appRefDir)
                        .Select(Path.GetFileName)
                        .Select(dir => new { Directory = dir, Version = TryParseVersion(dir, out var version) ? version : new Version() })
                        .Where(dir => dir.Version <= runtimeVersion)
                        .OrderByDescending(dirPair => dirPair.Version)
                        .Select(dirInfo => Path.Combine(appRefDir, dirInfo.Directory, "ref", $"net{dirInfo.Version.Major}.{dirInfo.Version.Minor}"))
                        .FirstOrDefault(Directory.Exists);
                if (latestRuntimeDirAndVersion is { })
                {
                    refAssemblyDir = latestRuntimeDirAndVersion;
                }
            }
        }

        return refAssemblyDir;
    }

    private static bool TryParseVersion(string versionString, out Version v)
    {
        var previewVersionOffset = versionString.IndexOf('-');
        if (previewVersionOffset >= 0)
        {
            // looks like a preview version of the SDK, e.g., 6.0.0-preview.7.21377.19 which `Version.TryParse()` can't handle, so we have to fake it
            versionString = versionString.Substring(0, previewVersionOffset);
        }

        return Version.TryParse(versionString, out v);
    }

    private static IReadOnlyCollection<MetadataReference> ResolveRefAssemblies()
    {
        var assemblyRefs = new List<MetadataReference>();
        foreach (var assemblyRef in Directory.EnumerateFiles(ResolveRefAssemblyPath(), "*.dll"))
        {
            try
            {
                var resolved = CachingMetadataResolver.ResolveReferenceWithXmlDocumentationProvider(assemblyRef, MetadataReferenceProperties.Assembly);
                assemblyRefs.Add(resolved);
            }
            catch (Exception ex) when (ex is ArgumentNullException or ArgumentException or IOException)
            {
                // the only exceptions that can be thrown by `ResolveReferenceWithXmlDocumentationProvider` which
                // internally calls `XmlDocumentationProvider.CreateFromFile`
            }
        }

        return assemblyRefs;
    }

    public async Task UpdateWorkspaceAsync(ScriptState scriptState)
    {
        try
        {
            await _workspaceLock.WaitAsync();

            _currentCompilation = scriptState.Script.GetCompilation();

            var solution = CurrentSolution;
            solution = RemoveWorkingProjectFromSolution(solution);

            SetCurrentSolution(solution);

            _previousSubmissionProjectId = AddProjectWithPreviousSubmissionToSolution(_currentCompilation,
                scriptState.Script.Code, _workingProjectId, _previousSubmissionProjectId);

            AddNewWorkingProjectToSolution(_currentCompilation, _previousSubmissionProjectId);
        }
        finally
        {
            _workspaceLock.Release();
        }
    }

    private Solution RemoveWorkingProjectFromSolution(Solution solution)
    {
        if (_workingProjectId is not null)
        {
            solution = solution.RemoveProject(_workingProjectId);
        }

        return solution;
    }

    private IReadOnlyCollection<MetadataReference> GetReferenceSet(Compilation compilation)
    {
        var references =
            _referenceAssemblies
                .Concat(_packageManagerReferences)
                .Concat(compilation.DirectiveReferences)
                .ToArray();
        return references;
    }

    private void AddNewWorkingProjectToSolution(Compilation previousCompilation, ProjectId projectReferenceProjectId)
    {
        var solution = CurrentSolution;
        var assemblyName = $"Submission#{_submissionCount++}";
        var compilationOptions = previousCompilation.Options.WithScriptClassName(assemblyName);

        _workingProjectId = ProjectId.CreateNewId(debugName: $"workingProject{assemblyName}");

        var references = GetReferenceSet(previousCompilation);

        solution = CreateProjectAndAddToSolution(
            _workingProjectId,
            assemblyName,
            compilationOptions,
            solution,
            projectReferenceProjectId,
            references);

        _workingDocumentId = DocumentId.CreateNewId(_workingProjectId);

        solution = solution.AddDocument(
            _workingDocumentId,
            "active",
            string.Empty);

        SetCurrentSolution(solution);
    }

    private Solution CreateProjectAndAddToSolution(
        ProjectId projectId,
        string assemblyName,
        CompilationOptions compilationOptions,
        Solution solution,
        ProjectId projectReferenceProjectId,
        IEnumerable<MetadataReference> metadataReferences = null)
    {
        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            name: assemblyName,
            assemblyName: assemblyName,
            language: LanguageNames.CSharp,
            parseOptions: _parseOptions,
            compilationOptions: compilationOptions,
            metadataReferences: metadataReferences,
            isSubmission: true);

        solution = solution.AddProject(projectInfo);

        if (projectReferenceProjectId is not null)
        {
            solution = solution.AddProjectReference(
                projectId,
                new ProjectReference(projectReferenceProjectId)
            );
        }

        return solution;
    }

    private ProjectId AddProjectWithPreviousSubmissionToSolution(Compilation compilation, string code, ProjectId projectId, ProjectId projectReferenceProjectId)
    {
        var solution = CurrentSolution;

        var compilationOptions = compilation.Options;
        var assemblyName = compilationOptions.ScriptClassName;

        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            assemblyName = $"Submission#{_submissionCount}";
            compilationOptions = compilationOptions.WithScriptClassName(assemblyName);
        }

        var debugName = assemblyName;
#if DEBUG
        debugName += $": {code}";
#endif
        if (projectId is null)
        {
            projectId = ProjectId.CreateNewId(debugName: debugName);
        }

        var references = GetReferenceSet(compilation);

        solution = CreateProjectAndAddToSolution(
            projectId,
            assemblyName,
            compilationOptions,
            solution,
            projectReferenceProjectId,
            references);

        var currentSubmissionDocumentId = DocumentId.CreateNewId(
            projectId,
            debugName: assemblyName);

        // add the code submission to the current project
        var submissionSourceText = SourceText.From(code);

        solution = solution.AddDocument(
            currentSubmissionDocumentId,
            debugName,
            submissionSourceText);

        SetCurrentSolution(solution);
        return projectId;
    }

    public Document ForkDocumentForLanguageServices(string code)
    {
        var solution = CurrentSolution;

        solution = solution.RemoveDocument(_workingDocumentId);

        var documentDebugName = $"Working from #{_submissionCount}";

        var workingDocumentId = DocumentId.CreateNewId(
            _workingProjectId,
            documentDebugName);

        solution = solution.AddDocument(
            workingDocumentId,
            documentDebugName,
            SourceText.From(code)
        );

        var workingDocument = solution.GetDocument(workingDocumentId);

        return workingDocument;
    }

    public void AddPackageManagerReference(MetadataReference reference)
    {
        _packageManagerReferences.Add(reference);
    }

    protected override void Dispose(bool finalize)
    {
        if (!finalize)
        {
            _disposables.Dispose();
        }
        base.Dispose(finalize);
    }
}