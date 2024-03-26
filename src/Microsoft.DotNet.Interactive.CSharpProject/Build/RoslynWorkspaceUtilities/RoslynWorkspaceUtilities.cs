// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using System;
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
        if (!File.Exists(cacheFilePath))
        {
            return null;
        }

        string fileContent = File.ReadAllText(cacheFilePath);

        PopulateBuildProjectData(fileContent, out var buildProjectData);

        var workspace = GetWorkspace(buildProjectData);

        var cSharpParseOptions = CreateCSharpParseOptions(buildProjectData);

        return new BuildDataResults
        {
            BuildProjectData = buildProjectData,
            ProjectFilePath = buildProjectData.ProjectFilePath,
            CacheFilePath = cacheFilePath,
            Workspace = workspace,
            Succeeded = true,
            CSharpParseOptions = cSharpParseOptions
        };
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
                        if (Guid.TryParse(value, out var projectGuid))
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

    public static AdhocWorkspace GetWorkspace(BuildProjectData buildProjectData)
    {
        if (buildProjectData is null)
        {
            throw new ArgumentNullException(nameof(buildProjectData));
        }

        if (buildProjectData is null)
        {
            throw new ArgumentNullException(nameof(buildProjectData));
        }

        var workspace = new AdhocWorkspace();

        var projectId = ProjectId.CreateFromSerialized(buildProjectData.ProjectGuid);

        var projectInfo = CreateProjectInfo(buildProjectData, workspace, projectId);
      
        var solution = workspace.CurrentSolution.AddProject(projectInfo);

        if (!workspace.TryApplyChanges(solution))
        {
            throw new InvalidOperationException("Could not apply workspace solution changes");
        }

        workspace.CurrentSolution.GetProject(projectId);

        return workspace;
    }

    private static ProjectInfo CreateProjectInfo(BuildProjectData buildProjectData, CodeAnalysis.Workspace workspace, ProjectId projectId)
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
        buildProjectData.SourceFiles is null
            ? Array.Empty<DocumentInfo>()
            : buildProjectData.SourceFiles
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

        return buildProjectData.AnalyzerReferences is null
                   ? Array.Empty<AnalyzerReference>()
                   : buildProjectData.AnalyzerReferences?
                                     .Where(File.Exists)
                                     .Select(x => new AnalyzerFileReference(x, loader)).ToList();
    }

    private static ParseOptions CreateParseOptions(BuildProjectData buildProjectData) => CreateCSharpParseOptions(buildProjectData);

    private static CSharpParseOptions CreateCSharpParseOptions(BuildProjectData buildProjectData)
    {
        var parseOptions = new CSharpParseOptions();

        parseOptions = parseOptions.WithPreprocessorSymbols(buildProjectData.PreprocessorSymbols);

        if (!string.IsNullOrWhiteSpace(buildProjectData.LangVersion) &&
            LanguageVersionFacts.TryParse(buildProjectData.LangVersion, out var languageVersion))
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

        return new CSharpCompilationOptions(kind.Value);
    }
}
