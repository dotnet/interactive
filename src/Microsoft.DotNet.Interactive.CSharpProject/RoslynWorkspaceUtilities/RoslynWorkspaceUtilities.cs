﻿using Markdig.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static Microsoft.DotNet.Interactive.CSharpProject.RoslynWorkspaceUtilities.BuildCacheFileUtilities;

namespace Microsoft.DotNet.Interactive.CSharpProject.RoslynWorkspaceUtilities;

internal static class RoslynWorkspaceUtilities
{
    internal static BuildDataResults GetResultsFromCacheFile(string cacheFilePath)
    {
        CodeAnalysis.Workspace ws = null;
        BuildProjectData buildProjectData = null;
        CSharpParseOptions cSharpParseOptions = null;

        try
        {
            if (!File.Exists(cacheFilePath))
            {
                return null;
            }

            string fileContent = File.ReadAllText(cacheFilePath);

            PopulateBuildProjectData(fileContent, out buildProjectData);

            ws = GetWorkspace(buildProjectData, true);

            cSharpParseOptions = CreateCSharpParseOptions(buildProjectData);

            return new BuildDataResults()
            {
                BuildProjectData = buildProjectData,
                ProjectFilePath = cacheFilePath,
                CacheFilePath = cacheFilePath + cacheFilenameSuffix,
                Workspace = ws,
                Succeeded = true,
                CSharpParseOptions = cSharpParseOptions
            };
        }
        catch (ArgumentNullException)
        {
            return null;
        }
    }

    internal static BuildDataResults ResultsFromCacheFileUsingProjectFilePath(string csprojFilePath)
    {
        if (!File.Exists(csprojFilePath))
        {
            throw new ArgumentException($"project file does not exist : {csprojFilePath}");
        }

        var cacheFilePath = csprojFilePath + cacheFilenameSuffix;

        return GetResultsFromCacheFile(cacheFilePath);
    }

    internal static void PopulateBuildProjectData(string fileContent, out BuildProjectData buildProjectData)
    {
        buildProjectData = new BuildProjectData();

        string[] lines = fileContent.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        var referencesList = new List<string>();
        var projectReferencesList = new List<string>();
        var analyzerReferencesList = new List<string>();
        var preprocessorSymbolsList = new List<string>();
        var sourceFilesList = new List<string>();

        foreach (string line in lines)
        {
            string[] parts = line.Split('=');
            if (parts.Length == 2)
            {
                string key = parts[0].Trim();
                string value = parts[1].Trim();

                switch (key)
                {
                    case "ProjectGuid":
                        if (Guid.TryParse(value, out Guid projectGuid))
                        {
                            buildProjectData.ProjectGuid = projectGuid;
                        }
                        else
                        {
                            // Net Core projects do not provide a project guid, so lets create one.
                            // Project guid is not relevant for build.
                            buildProjectData.ProjectGuid = Guid.NewGuid();
                        }
                        break;
                    case "ProjectReferences":
                        projectReferencesList.Add(value);
                        break;
                    case "ProjectFilePath":
                        buildProjectData.ProjectFilePath = value;
                        break;
                    case "LanguageName":
                        buildProjectData.LanguageName = value;
                        break;
                    case "PropertyTargetPath":
                        buildProjectData.TargetPath = value;
                        break;
                    case "SourceFiles":
                        sourceFilesList.Add(value);
                        break;
                    case "References":
                        referencesList.Add(value);
                        break;
                    case "AnalyzerReferences":
                        analyzerReferencesList.Add(value);
                        break;
                    case "PreprocessorSymbols":
                        preprocessorSymbolsList.Add(value);
                        break;
                    case "PropertyLangVersion":
                        buildProjectData.LangVersion = value;
                        break;
                    case "PropertyOutputType":
                        buildProjectData.OutputType = value;
                        break;
                    default:
                        break;
                }
            }
        }

        buildProjectData.References = referencesList.ToArray();
        buildProjectData.ProjectReferences = projectReferencesList.ToArray();
        buildProjectData.AnalyzerReferences = analyzerReferencesList.ToArray();
        buildProjectData.PreprocessorSymbols = preprocessorSymbolsList.ToArray();
        buildProjectData.SourceFiles = sourceFilesList.ToArray();
    }

    public static AdhocWorkspace GetWorkspace(BuildProjectData buildProjectData, bool addProjectReferences = false)
    {
        if (buildProjectData is null)
        {
            throw new ArgumentNullException(nameof(buildProjectData));
        }
        AdhocWorkspace workspace = CreateWorkspace();
        AddToWorkspace(buildProjectData, workspace, addProjectReferences);
        return workspace;
    }

    public static CodeAnalysis.Project AddToWorkspace(BuildProjectData analyzerResult, CodeAnalysis.Workspace workspace, bool addProjectReferences = false)
    {
        if (analyzerResult is null)
        {
            throw new ArgumentNullException(nameof(analyzerResult));
        }
        if (workspace is null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        ProjectId projectId = ProjectId.CreateFromSerialized(analyzerResult.ProjectGuid);

        ConcurrentDictionary<Guid, string[]> WorkspaceProjectReferences = new ConcurrentDictionary<Guid, string[]>();
        WorkspaceProjectReferences[projectId.Id] = analyzerResult.ProjectReferences.ToArray();

        ProjectInfo projectInfo = GetProjectInfo(analyzerResult, workspace, projectId);
        if (projectInfo is null)
        {
            throw new ArgumentNullException(nameof(projectInfo));
        }
        Solution solution = workspace.CurrentSolution.AddProject(projectInfo);

        if (!workspace.TryApplyChanges(solution))
        {
            throw new InvalidOperationException("Could not apply workspace solution changes");
        }

        return workspace.CurrentSolution.GetProject(projectId);
    }

    internal static AdhocWorkspace CreateWorkspace()
    {
        AdhocWorkspace workspace = new AdhocWorkspace();
        workspace.WorkspaceChanged += WorkspaceChangedHandler;
        workspace.WorkspaceFailed += WorkspaceFailedHandler;
        return workspace;
    }

    private static void WorkspaceFailedHandler(object sender, WorkspaceDiagnosticEventArgs e)
    {
        // Log error
    }

    private static void WorkspaceChangedHandler(object sender, WorkspaceChangeEventArgs e)
    {
        // Log
    }

    private static ProjectInfo GetProjectInfo(BuildProjectData buildProjectData, CodeAnalysis.Workspace workspace, ProjectId projectId)
    {
        string projectName = Path.GetFileNameWithoutExtension(buildProjectData.ProjectFilePath);

        return ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            projectName,
            projectName,
            LanguageNames.CSharp,
            filePath: buildProjectData.ProjectFilePath,
            outputFilePath: buildProjectData.TargetPath,
            documents: GetDocuments(buildProjectData, projectId),
            projectReferences: GetExistingProjectReferences(buildProjectData, workspace),
            metadataReferences: GetMetadataReferences(buildProjectData),
            analyzerReferences: GetAnalyzerReferences(buildProjectData, workspace),
            parseOptions: CreateParseOptions(buildProjectData),
            compilationOptions: CreateCompilationOptions(buildProjectData));
    }

    private static IReadOnlyList<DocumentInfo> GetDocuments(BuildProjectData buildProjectData, ProjectId projectId) =>
        buildProjectData.SourceFiles is null ?
            Array.Empty<DocumentInfo>() :
            buildProjectData.SourceFiles
                .Where(File.Exists)
                .Select(x => DocumentInfo.Create(
                    DocumentId.CreateNewId(projectId),
                    Path.GetFileName(x),
                    loader: TextLoader.From(
                        TextAndVersion.Create(
                        SourceText.From(File.ReadAllText(x), Encoding.Unicode), VersionStamp.Create())),
                    filePath: x)).ToList();

    private static IReadOnlyList<ProjectReference> GetExistingProjectReferences(BuildProjectData buildProjectData, CodeAnalysis.Workspace workspace) =>
        buildProjectData.ProjectReferences
            .Select(x => workspace.CurrentSolution.Projects.FirstOrDefault(y => y.FilePath.Equals(x, StringComparison.OrdinalIgnoreCase)))
            .Where(x => x != null)
            .Select(x => new ProjectReference(x.Id)).ToList();

    private static IReadOnlyList<MetadataReference> GetMetadataReferences(BuildProjectData buildProjectData) =>
        buildProjectData
            .References?.Where(File.Exists)
            .Select(x => MetadataReference.CreateFromFile(x)).ToList();

    private static IReadOnlyList<AnalyzerReference> GetAnalyzerReferences(BuildProjectData buildProjectData, CodeAnalysis.Workspace workspace)
    {
        IAnalyzerAssemblyLoader loader = workspace.Services.GetRequiredService<IAnalyzerService>().GetLoader();

        return buildProjectData.AnalyzerReferences is null ?
            Array.Empty<AnalyzerReference>() :
            buildProjectData.AnalyzerReferences?
                .Where(File.Exists)
                .Select(x => new AnalyzerFileReference(x, loader)).ToList();
    }

    private static ParseOptions CreateParseOptions(BuildProjectData buildProjectData) => CreateCSharpParseOptions(buildProjectData);

    private static CSharpParseOptions CreateCSharpParseOptions(BuildProjectData buildProjectData)
    {
        CSharpParseOptions parseOptions = new CSharpParseOptions();

        parseOptions = parseOptions.WithPreprocessorSymbols(buildProjectData.PreprocessorSymbols);

        string langVersion = buildProjectData.LangVersion;
        if (!string.IsNullOrWhiteSpace(langVersion)
            && LanguageVersionFacts.TryParse(langVersion, out LanguageVersion languageVersion))
        {
            parseOptions = parseOptions.WithLanguageVersion(languageVersion);
        }

        return parseOptions;
    }

    private static CompilationOptions CreateCompilationOptions(BuildProjectData buildProjectData)
    {
        string outputType = buildProjectData.OutputType;
        OutputKind? kind = null;
        switch (outputType)
        {
            case "Library":
                kind = OutputKind.DynamicallyLinkedLibrary;
                break;
            case "Exe":
                kind = OutputKind.ConsoleApplication;
                break;
            case "Module":
                kind = OutputKind.NetModule;
                break;
            case "Winexe":
                kind = OutputKind.WindowsApplication;
                break;
        }

        CompilationOptions CreateCSharpCompilationOptions() => new CSharpCompilationOptions(kind.Value);
        return CreateCSharpCompilationOptions();
    }
}
