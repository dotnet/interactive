// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.CSharpProject.Build;

internal class BuildResult
{
    internal ProjectBuildInfo ProjectBuildInfo { get; init; }

    internal bool Succeeded { get; init; }

    internal string CacheFilePath { get; init; }

    internal CSharpParseOptions CSharpParseOptions { get; init; }

    internal string ProjectFilePath { get; init; }

    internal static BuildResult FromCacheFile(string cacheFilePath)
    {
        if (!File.Exists(cacheFilePath))
        {
            return null;
        }

        var fileContent = File.ReadAllText(cacheFilePath);

        PopulateBuildProjectData(fileContent, out var buildProjectData);

        var cSharpParseOptions = CreateCSharpParseOptions(buildProjectData);

        return new BuildResult
        {
            ProjectBuildInfo = buildProjectData,
            ProjectFilePath = buildProjectData.ProjectFilePath,
            CacheFilePath = cacheFilePath,
            Succeeded = true,
            CSharpParseOptions = cSharpParseOptions
        };
    }

    internal static void PopulateBuildProjectData(string fileContent, out ProjectBuildInfo projectBuildInfo)
    {
        projectBuildInfo = new ProjectBuildInfo();

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
                            projectBuildInfo.ProjectGuid = projectGuid;
                        }
                        else
                        {
                            // Net Core projects do not provide a project guid, so lets create one.
                            // Project guid is not relevant for build.
                            projectBuildInfo.ProjectGuid = Guid.NewGuid();
                        }
                        break;
                    case "ProjectReferences":
                        projectReferencesList.Add(value);
                        break;
                    case "ProjectFilePath":
                        projectBuildInfo.ProjectFilePath = value;
                        break;
                    case "LanguageName":
                        projectBuildInfo.LanguageName = value;
                        break;
                    case "PropertyTargetPath":
                        projectBuildInfo.TargetPath = value;
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
                        projectBuildInfo.LangVersion = value;
                        break;
                    case "PropertyOutputType":
                        projectBuildInfo.OutputType = value;
                        break;
                    default:
                        break;
                }
            }
        }

        projectBuildInfo.References = referencesList.ToArray();
        projectBuildInfo.ProjectReferences = projectReferencesList.ToArray();
        projectBuildInfo.AnalyzerReferences = analyzerReferencesList.ToArray();
        projectBuildInfo.PreprocessorSymbols = preprocessorSymbolsList.ToArray();
        projectBuildInfo.SourceFiles = sourceFilesList.ToArray();
    }

    public AdhocWorkspace CreateWorkspace()
    {
        var projectBuildInfo = ProjectBuildInfo;

        var projectId = ProjectId.CreateFromSerialized(projectBuildInfo.ProjectGuid);

        var workspace = new AdhocWorkspace();

        var projectInfo = CreateProjectInfo(projectBuildInfo, workspace, projectId);

        var solution = workspace.CurrentSolution.AddProject(projectInfo);

        if (!workspace.TryApplyChanges(solution))
        {
            throw new InvalidOperationException("Could not apply workspace solution changes");
        }

        workspace.CurrentSolution.GetProject(projectId);

        return workspace;
    }

    private static ProjectInfo CreateProjectInfo(
        ProjectBuildInfo projectBuildInfo, 
        CodeAnalysis.Workspace workspace, 
        ProjectId projectId)
    {
        var projectName = Path.GetFileNameWithoutExtension(projectBuildInfo.ProjectFilePath);

        return ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            projectName,
            projectName,
            LanguageNames.CSharp,
            filePath: projectBuildInfo.ProjectFilePath,
            outputFilePath: projectBuildInfo.TargetPath,
            documents: GetDocuments(projectBuildInfo, projectId),
            projectReferences: GetExistingProjectReferences(projectBuildInfo, workspace),
            metadataReferences: GetMetadataReferences(projectBuildInfo),
            analyzerReferences: GetAnalyzerReferences(projectBuildInfo, workspace),
            parseOptions: CreateParseOptions(projectBuildInfo),
            compilationOptions: CreateCompilationOptions(projectBuildInfo));
    }

    private static IReadOnlyList<DocumentInfo> GetDocuments(ProjectBuildInfo projectBuildInfo, ProjectId projectId) =>
        projectBuildInfo.SourceFiles is null
            ? Array.Empty<DocumentInfo>()
            : projectBuildInfo.SourceFiles
                              .Where(File.Exists)
                              .Select(x => DocumentInfo.Create(
                                          DocumentId.CreateNewId(projectId),
                                          Path.GetFileName(x),
                                          loader: TextLoader.From(
                                              TextAndVersion.Create(
                                                  SourceText.From(File.ReadAllText(x), Encoding.Unicode), VersionStamp.Create())),
                                          filePath: x)).ToList();

    private static IReadOnlyList<ProjectReference> GetExistingProjectReferences(ProjectBuildInfo projectBuildInfo, CodeAnalysis.Workspace workspace) =>
        projectBuildInfo.ProjectReferences
                        .Select(x => workspace.CurrentSolution.Projects.FirstOrDefault(y => y.FilePath.Equals(x, StringComparison.OrdinalIgnoreCase)))
                        .Where(x => x != null)
                        .Select(x => new ProjectReference(x.Id)).ToList();

    private static IReadOnlyList<MetadataReference> GetMetadataReferences(ProjectBuildInfo projectBuildInfo) =>
        projectBuildInfo
            .References?.Where(File.Exists)
            .Select(x => MetadataReference.CreateFromFile(x)).ToList();

    private static IReadOnlyList<AnalyzerReference> GetAnalyzerReferences(ProjectBuildInfo projectBuildInfo, CodeAnalysis.Workspace workspace)
    {
        IAnalyzerAssemblyLoader loader = workspace.Services.GetRequiredService<IAnalyzerService>().GetLoader();

        return projectBuildInfo.AnalyzerReferences is null
                   ? Array.Empty<AnalyzerReference>()
                   : projectBuildInfo.AnalyzerReferences?
                                     .Where(File.Exists)
                                     .Select(x => new AnalyzerFileReference(x, loader)).ToList();
    }

    private static ParseOptions CreateParseOptions(ProjectBuildInfo projectBuildInfo) => CreateCSharpParseOptions(projectBuildInfo);

    private static CSharpParseOptions CreateCSharpParseOptions(ProjectBuildInfo projectBuildInfo)
    {
        var parseOptions = new CSharpParseOptions();

        parseOptions = parseOptions.WithPreprocessorSymbols(projectBuildInfo.PreprocessorSymbols);

        if (!string.IsNullOrWhiteSpace(projectBuildInfo.LangVersion) &&
            LanguageVersionFacts.TryParse(projectBuildInfo.LangVersion, out var languageVersion))
        {
            parseOptions = parseOptions.WithLanguageVersion(languageVersion);
        }

        return parseOptions;
    }

    private static CompilationOptions CreateCompilationOptions(ProjectBuildInfo projectBuildInfo)
    {
        string outputType = projectBuildInfo.OutputType;
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