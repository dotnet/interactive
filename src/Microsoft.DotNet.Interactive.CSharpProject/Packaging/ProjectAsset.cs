// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;
using static Microsoft.DotNet.Interactive.CSharpProject.RoslynWorkspaceUtilities.RoslynWorkspaceUtilities;
using Microsoft.DotNet.Interactive.CSharpProject.RoslynWorkspaceUtilities;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging;

public class ProjectAsset : PackageAsset,
    ICreateWorkspaceForLanguageServices,
    ICreateWorkspaceForRun,
    IHaveADirectory
{
    private const string FullBuildBinlogFileName = "package_fullBuild.binlog";
    private readonly FileInfo _projectFile;
    private readonly FileInfo _lastBuildErrorLogFile;
    private readonly PipelineStep<BuildDataResults> _projectBuildStep;
    private readonly PipelineStep<CodeAnalysis.Workspace> _workspaceStep;
    private readonly PipelineStep<BuildDataResults> _cleanupStep;

    public string Name { get; }
        
    public DirectoryInfo Directory { get; }

    public ProjectAsset(IDirectoryAccessor directoryAccessor, string csprojFileName = null) : base(directoryAccessor)
    {
        if (directoryAccessor == null)
        {
            throw new ArgumentNullException(nameof(directoryAccessor));
        }

        if (string.IsNullOrWhiteSpace(csprojFileName))
        {
            var firstProject = DirectoryAccessor.GetAllFiles().Single(f => f.Extension == ".csproj");
            _projectFile = DirectoryAccessor.GetFullyQualifiedFilePath(firstProject.FileName);
        }
        else
        {
            _projectFile = DirectoryAccessor.GetFullyQualifiedFilePath(csprojFileName);
        }

        Directory = DirectoryAccessor.GetFullyQualifiedRoot();
        Name = _projectFile?.Name ?? Directory?.Name;
        _lastBuildErrorLogFile = directoryAccessor.GetFullyQualifiedFilePath(".net-interactive-builderror");
        _cleanupStep = new PipelineStep<BuildDataResults>(LoadResultOrCleanAsync);
        _projectBuildStep = _cleanupStep.Then(BuildProjectAsync);
        _workspaceStep = _projectBuildStep.Then(BuildWorkspaceAsync);
    }

    private async Task<BuildDataResults> LoadResultOrCleanAsync()
    {
        using (await DirectoryAccessor.TryLockAsync())
        {
            var binLog = this.FindLatestBinLog();
            if (binLog != null)
            {
                var results = await TryLoadAnalyzerResultsAsync(binLog);

                var didCompile = DidPerformCoreCompile(results);
                if (results != null)
                {
                    if (results.Succeeded && didCompile)
                    {
                        return results;
                    }
                }
            }

            binLog?.DoWhenFileAvailable(() => binLog.Delete());
            var toClean = Directory.GetDirectories("obj");
            foreach (var directoryInfo in toClean)
            {
                directoryInfo.Delete(true);
            }

            return null;
        }
    }

    private bool DidPerformCoreCompile(BuildDataResults result)
    {
        if (result == null)
        {
            return false;
        }

        var sourceCount = result.BuildProjectData.SourceFiles?.Length ?? 0;
        var compilerInputs = result.BuildProjectData.CompileInputs?.Length ?? 0;

        return compilerInputs > 0 && sourceCount > 0;
    }

    private CodeAnalysis.Workspace BuildWorkspaceAsync(BuildDataResults result)
    {
        var ws = result.Workspace;
        var projectId = ws.CurrentSolution.ProjectIds.FirstOrDefault();
        var references = result.BuildProjectData.References;
        var metadataReferences = references.GetMetadataReferences();
        var solution = ws.CurrentSolution;
        solution = solution.WithProjectMetadataReferences(projectId, metadataReferences);
        ws.TryApplyChanges(solution);
        return ws;
    }

    private async Task<BuildDataResults> BuildProjectAsync(BuildDataResults result)
    {
        if (result is { })
        {
            return result;
        }

        using var _ = await DirectoryAccessor.TryLockAsync();

        await DotnetBuild();

        var binLog = this.FindLatestBinLog();

        if (binLog == null)
        {
            throw new InvalidOperationException("Failed to build");
        }

        var results = await TryLoadAnalyzerResultsAsync(binLog);

        if (results is null)
        {
            throw new InvalidOperationException("The build log seems to contain no solutions or projects");
        }

        if (results.Succeeded == true)
        {
            return result;
        }

        throw new InvalidOperationException("Failed to build");
    }

    private async Task<BuildDataResults> TryLoadAnalyzerResultsAsync(FileInfo binLog)
    {
        BuildDataResults results = null;
        await binLog.DoWhenFileAvailable(() =>
        {
            results = ResultsFromCacheFileUsingProjectFilePath(binLog.FullName);
        });
        return results;
    }

    public Task<CodeAnalysis.Workspace> CreateWorkspaceAsync()
    {
        return _workspaceStep.GetLatestAsync();
    }

    public Task<CodeAnalysis.Workspace> CreateWorkspaceForRunAsync()
    {
        return CreateWorkspaceAsync();
    }

    public Task<CodeAnalysis.Workspace> CreateWorkspaceForLanguageServicesAsync()
    {
        return CreateWorkspaceAsync();
    }

    protected async Task DotnetBuild()
    {
        var args = $"/bl:{FullBuildBinlogFileName}";
        if (_projectFile?.Exists == true)
        {
            args = $@"""{_projectFile.FullName}"" {args}";
        }

        var result = await new Dotnet(Directory).Build(args: args);

        if (result.ExitCode != 0)
        {
            File.WriteAllText(
                _lastBuildErrorLogFile.FullName,
                string.Join(Environment.NewLine, result.Error));
        }
        else if (_lastBuildErrorLogFile.Exists)
        {
            _lastBuildErrorLogFile.Delete();
        }

        result.ThrowOnFailure();
    }
}