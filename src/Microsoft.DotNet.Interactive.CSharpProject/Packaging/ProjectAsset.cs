// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Buildalyzer;
using Clockwise;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Interactive.CSharpProject.Tools;
using Pocket;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging
{
    public class ProjectAsset : PackageAsset,
        ICreateWorkspaceForLanguageServices,
        ICreateWorkspaceForRun,
        IHaveADirectory
    {
        private const string FullBuildBinlogFileName = "package_fullBuild.binlog";
        private readonly FileInfo _projectFile;
        private readonly FileInfo _lastBuildErrorLogFile;
        private readonly PipelineStep<IAnalyzerResult> _projectBuildStep;
        private readonly PipelineStep<Workspace> _workspaceStep;
        private readonly PipelineStep<IAnalyzerResult> _cleanupStep;

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
            _lastBuildErrorLogFile = directoryAccessor.GetFullyQualifiedFilePath(".trydotnet-builderror");
            _cleanupStep = new PipelineStep<IAnalyzerResult>(LoadResultOrCleanAsync);
            _projectBuildStep = _cleanupStep.Then(BuildProjectAsync);
            _workspaceStep = _projectBuildStep.Then(BuildWorkspaceAsync);
        }

        private async Task<IAnalyzerResult> LoadResultOrCleanAsync()
        {
            using (await DirectoryAccessor.TryLockAsync())
            {
                var binLog = this.FindLatestBinLog();
                if (binLog != null)
                {
                    var results = await TryLoadAnalyzerResultsAsync(binLog);
                    var result = results?.FirstOrDefault(p => p.ProjectFilePath == _projectFile.FullName);

                    var didCompile = DidPerformCoreCompile(result);
                    if (result != null)
                    {
                        if (result.Succeeded && didCompile)
                        {
                            return result;
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

        private bool DidPerformCoreCompile(IAnalyzerResult result)
        {
            if (result == null)
            {
                return false;
            }

            var sourceCount = result.SourceFiles?.Length ?? 0;
            var compilerInputs = result.GetCompileInputs()?.Length ?? 0;

            return compilerInputs > 0 && sourceCount > 0;
        }

        private Task<Workspace> BuildWorkspaceAsync(IAnalyzerResult result)
        {
            if (result.TryGetWorkspace(out var ws))
            {
                var projectId = ws.CurrentSolution.ProjectIds.FirstOrDefault();
                var references = result.References;
                var metadataReferences = references.GetMetadataReferences();
                var solution = ws.CurrentSolution;
                solution = solution.WithProjectMetadataReferences(projectId, metadataReferences);
                ws.TryApplyChanges(solution);
                return Task.FromResult(ws);
            }
            throw new InvalidOperationException("Failed creating workspace");
        }

        private async Task<IAnalyzerResult> BuildProjectAsync(IAnalyzerResult result)
        {
            if (result != null)
            {
                return result;
            }

            using (await DirectoryAccessor.TryLockAsync())
            {

                {
                    await DotnetBuild();

                    var binLog = this.FindLatestBinLog();

                    if (binLog == null)
                    {
                        throw new InvalidOperationException("Failed to build");
                    }

                    var results = await TryLoadAnalyzerResultsAsync(binLog);

                    if (results?.Count == 0)
                    {
                        throw new InvalidOperationException("The build log seems to contain no solutions or projects");
                    }

                    result = results?.FirstOrDefault(p => p.ProjectFilePath == _projectFile.FullName);

                    if (result?.Succeeded == true)
                    {
                        return result;
                    }

                    throw new InvalidOperationException("Failed to build");
                }
            }
        }

        private async Task<IAnalyzerResults> TryLoadAnalyzerResultsAsync(FileInfo binLog)
        {
            IAnalyzerResults results = null;
            await binLog.DoWhenFileAvailable(() =>
            {
                var manager = new AnalyzerManager();
                results = manager.Analyze(binLog.FullName);
            });
            return results;
        }

        public Task<Workspace> CreateRoslynWorkspaceAsync(Budget budget)
        {
            return _workspaceStep.GetLatestAsync();
        }

        public Task<Workspace> CreateRoslynWorkspaceForRunAsync(Budget budget)
        {
            return CreateRoslynWorkspaceAsync(budget);
        }

        public Task<Workspace> CreateRoslynWorkspaceForLanguageServicesAsync(Budget budget)
        {
            return CreateRoslynWorkspaceAsync(budget);
        }


        protected async Task DotnetBuild()
        {
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
    }
}