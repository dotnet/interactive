// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.CSharpProject.RoslynWorkspaceUtilities;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.DotNet.Interactive.CSharpProject.Build.RoslynWorkspaceUtilities.RoslynWorkspaceUtilities;
using static Pocket.Logger<Microsoft.DotNet.Interactive.CSharpProject.Build.Prebuild>;
using Disposable = System.Reactive.Disposables.Disposable;

namespace Microsoft.DotNet.Interactive.CSharpProject.Build;

public class Prebuild 
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _buildSemaphores = new();

    static Prebuild()
    {
        const string PrebuildPathEnvironmentVariableName = "TRYDOTNET_PREBUILDS_PATH";

        var environmentVariable = Environment.GetEnvironmentVariable(PrebuildPathEnvironmentVariableName);

        DefaultPrebuildsDirectory =
            environmentVariable is not null
                ? new DirectoryInfo(environmentVariable)
                : new DirectoryInfo(
                    Path.Combine(
                        Paths.UserProfile,
                        ".trydotnet",
                        "prebuilds"));

        if (!DefaultPrebuildsDirectory.Exists)
        {
            DefaultPrebuildsDirectory.Create();
        }

        Log.Info("Prebuilds path is {DefaultWorkspacesDirectory}", DefaultPrebuildsDirectory);
    }

    private readonly AsyncLazy<bool> _lazyCreation;

    private int buildCount = 0;

    private FileInfo _entryPointAssemblyPath;
    private static string _targetFramework;
    private readonly Logger _log;
    private readonly Subject<Unit> _buildRequestChannel;

    protected CodeAnalysis.Workspace _roslynWorkspace;

    private readonly IScheduler _buildThrottleScheduler;
    private readonly SerialDisposable _buildThrottleSubscription;

    private BuildDataResults _lastBuildResult;

    private readonly FileInfo _lastBuildErrorLogFile;

    private TaskCompletionSource<CodeAnalysis.Workspace> _buildCompletionSource = new();

    private readonly object _buildCompletionSourceLock = new();

    public Prebuild(
        string name,
        IPrebuildInitializer initializer = null,
        DirectoryInfo directory = null,
        bool enableBuild = false)
    {
        Name = name;
        EnableBuild = enableBuild;
        Directory = directory ?? new DirectoryInfo(Path.Combine(DefaultPrebuildsDirectory.FullName, Name));
        var prebuildInitializer = initializer ?? new PrebuildInitializer("console", Name);

        _lazyCreation = new AsyncLazy<bool>(() => CreatePrebuildAsync(prebuildInitializer));
        _lastBuildErrorLogFile = new FileInfo(Path.Combine(Directory.FullName, ".net-interactive-builderror"));

        _log = new Logger($"{nameof(Prebuild)}:{Name}");
        _buildThrottleScheduler = TaskPoolScheduler.Default;

        _buildRequestChannel = new Subject<Unit>();
        _buildThrottleSubscription = new SerialDisposable();
        
        InitializeBuildChannel();

        if (!TryLoadWorkspaceFromCache())
        {
            if (!EnableBuild)
            {
                throw new InvalidOperationException($"Prebuild not found at {Directory} and build-on-demand is disabled.");
            }
        }
    }

    public bool EnableBuild { get; }

    public DirectoryInfo Directory { get; }

    public string Name { get; }

    private Task<bool> EnsureCreatedAsync() => _lazyCreation.ValueAsync();

    private bool TryLoadWorkspaceFromCache()
    {
        if (Directory.Exists)
        {
            var cacheFile = FindCacheFile(Directory);
            
            if (cacheFile is not null)
            {
                LoadRoslynWorkspaceFromCache(cacheFile).Wait();
                return true;
            }
        }

        return false;
    }

    private async Task LoadRoslynWorkspaceFromCache(FileSystemInfo cacheFile)
    {
        var projectFile = GetProjectFile();

        if (projectFile is not null &&
            cacheFile.LastWriteTimeUtc >= projectFile.LastWriteTimeUtc)
        {
            BuildDataResults result;
            using (await FileLock.TryCreateAsync(Directory))
            {
                result = GetResultsFromCacheFile(cacheFile.FullName);
            }

            if (result is null)
            {
                throw new InvalidOperationException("The cache file contains no solutions or projects");
            }

            _roslynWorkspace = null;
            _lastBuildResult = result;

            if (result.Succeeded)
            {
                _roslynWorkspace = CreateRoslynWorkspace();
            }
        }
    }

    private FileInfo GetProjectFile() => Directory.GetFiles("*.csproj").FirstOrDefault();
    
    public static DirectoryInfo DefaultPrebuildsDirectory { get; }

    public FileInfo EntryPointAssemblyPath => 
        _entryPointAssemblyPath ??= this.GetEntryPointAssemblyPath();

    public string TargetFramework => 
        _targetFramework ??= this.GetTargetFramework();

    public async Task<CodeAnalysis.Workspace> GetOrCreateWorkspaceAsync()
    {
        if (_roslynWorkspace is { } ws)
        {
             return ws;
        }
        
        CreateCompletionSourceIfNeeded(ref _buildCompletionSource, _buildCompletionSourceLock);

        _buildRequestChannel.OnNext(Unit.Default);

        var newWorkspace = await _buildCompletionSource.Task;

        return newWorkspace;
    }
    
    private void CreateCompletionSourceIfNeeded(ref TaskCompletionSource<CodeAnalysis.Workspace> completionSource, object lockObject)
    {
        lock (lockObject)
        {
            switch (completionSource.Task.Status)
            {
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                case TaskStatus.RanToCompletion:
                    completionSource = new TaskCompletionSource<CodeAnalysis.Workspace>();
                    break;
            }
        }
    }

    private void SetCompletionSourceResult(TaskCompletionSource<CodeAnalysis.Workspace> completionSource, CodeAnalysis.Workspace result, object lockObject)
    {
        lock (lockObject)
        {
            switch (completionSource.Task.Status)
            {
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                case TaskStatus.RanToCompletion:
                    return;
                default:
                    completionSource.SetResult(result);
                    break;
            }
        }
    }

    private void SetCompletionSourceException(TaskCompletionSource<CodeAnalysis.Workspace> completionSource, Exception exception, object lockObject)
    {
        lock (lockObject)
        {
            switch (completionSource.Task.Status)
            {
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                case TaskStatus.RanToCompletion:
                    return;
                default:
                    completionSource.SetException(exception);
                    break;
            }
        }
    }

    private void InitializeBuildChannel()
    {
        _buildThrottleSubscription.Disposable = _buildRequestChannel
            .Throttle(TimeSpan.FromSeconds(0.5), _buildThrottleScheduler)
            .ObserveOn(TaskPoolScheduler.Default)
            .Subscribe(
                async _ =>
                {
                    try
                    {
                        await ProcessBuildRequest();
                    }
                    catch (Exception e)
                    {
                        SetCompletionSourceException(_buildCompletionSource, e, _buildCompletionSourceLock);
                    }
                },
                error =>
                {
                    SetCompletionSourceException(_buildCompletionSource, error, _buildCompletionSourceLock);
                    InitializeBuildChannel();
                });
    }

    private async Task ProcessBuildRequest()
    {
        await EnsureReadyAsync();
        var ws = CreateRoslynWorkspace();
        SetCompletionSourceResult(_buildCompletionSource, ws, _buildCompletionSourceLock);
    }

    private CodeAnalysis.Workspace CreateRoslynWorkspace()
    {
        var build = _lastBuildResult;

        if (build is null)
        {
            throw new InvalidOperationException("No design time or full build available");
        }

        var ws = build.Workspace;

        if (!ws.CanBeUsedToGenerateCompilation())
        {
            _roslynWorkspace = null;
            _lastBuildResult = null;
            throw new InvalidOperationException("The Roslyn workspace cannot be used to generate a compilation");
        }

        var projectId = ws.CurrentSolution.ProjectIds.FirstOrDefault();
        var references = build.BuildProjectData.References;
        var metadataReferences = references.GetMetadataReferences();
        var solution = ws.CurrentSolution;
        solution = solution.WithProjectMetadataReferences(projectId, metadataReferences);
        ws.TryApplyChanges(solution);
        _roslynWorkspace = ws;
        return ws;
    }

    public async Task EnsureReadyAsync()
    {
        if (_roslynWorkspace is not null)
        {
            Log.Info("Workspace already loaded for prebuild {name}.", Name);
            return;
        }

        await EnsureCreatedAsync();

        await EnsureBuiltAsync();
    }

    protected async Task EnsureBuiltAsync()
    {
        using var operation = _log.OnEnterAndConfirmOnExit();

        await EnsureCreatedAsync();

        if (IsBuildNeeded())
        {
            await BuildAsync();
        }
        else
        {
            operation.Info("Prebuild already built");
        }

        operation.Succeed();
    }

    public async Task BuildAsync()
    {
        using var operation = Log.OnEnterAndConfirmOnExit();

        if (!EnableBuild)
        {
            throw new InvalidOperationException($"Full build is disabled for prebuild '{this}'");
        }

        var buildSemaphore = _buildSemaphores.GetOrAdd(Name, _ => new SemaphoreSlim(1, 1));

        try
        {
            await EnsureCreatedAsync();

            operation.Info("Building prebuild '{name}'", Name);

            // When a build finishes, buildCount is reset to 0. If, when we increment
            // the value, we get a value > 1, someone else has already started another
            // build
            var buildInProgress = Interlocked.Increment(ref buildCount) > 1;

            await buildSemaphore.WaitAsync();

            using (Disposable.Create(() => buildSemaphore.Release()))
            {
                if (buildInProgress)
                {
                    operation.Info("Skipping build for prebuild '{name}'", Name);
                    return;
                }

                using (await FileLock.TryCreateAsync(Directory))
                {
                    await DotnetBuildAsync();
                }
            }

            operation.Info("Prebuild built");

            operation.Succeed();
        }
        catch (Exception exception)
        {
            operation.Error($"Exception building prebuild: {this}", exception);
        }

        var cacheFile = FindCacheFile(Directory);

        if (cacheFile is not { Exists: true })
        {
            throw new FileNotFoundException($"Cache file *.{CacheFilenameSuffix} not found in {Directory}.");
        }

        await cacheFile.WaitForFileAvailableAsync();
        await LoadRoslynWorkspaceFromCache(cacheFile);

        Interlocked.Exchange(ref buildCount, 0);
    }

    private async Task DotnetBuildAsync()
    {
        CleanObjFolder(Directory);

        var projectFile = GetProjectFile();

        var args = "";
        if (projectFile.Exists)
        {
            args = $"""
                "{projectFile.FullName}" {args}
                """;
        }

        var result = await new Dotnet(Directory).Build(args: args);

        if (result.ExitCode is not 0)
        {
            await File.WriteAllTextAsync(
                _lastBuildErrorLogFile.FullName,
                string.Join(Environment.NewLine, result.Error));
        }
        else if (_lastBuildErrorLogFile.Exists)
        {
            _lastBuildErrorLogFile.Delete();
        }

        result.ThrowOnFailure();
    }
    
    public override string ToString() => $"{Name} ({Directory.FullName})";

    protected virtual bool IsBuildNeeded() => _roslynWorkspace is null;
    
    public async Task<bool> CreatePrebuildAsync(IPrebuildInitializer initializer)
    {
        using var operation = Log.OnEnterAndConfirmOnExit();

        if (!EnableBuild)
        {
            throw new InvalidOperationException($"Full build is disabled for prebuild '{this}'");
        }

        if (!Directory.Exists)
        {
            operation.Info("Creating directory {directory}", Directory);
            Directory.Create();
            Directory.Refresh();
        }

        using (await FileLock.TryCreateAsync(Directory))
        {
            operation.Info("Initializing prebuild using {_initializer} in {directory}", initializer, Directory);
            await initializer.InitializeAsync(Directory);
        }

        operation.Succeed();

        return true;
    }

    public static async Task<Prebuild> GetOrCreateConsolePrebuildAsync(bool enableBuild = false)
    {
        var builder = new PrebuildBuilder("console");
        builder.UseTemplate("console");
        builder.UseLanguageVersion("latest");
        builder.AddPackageReference("Newtonsoft.Json", "13.0.3");
        builder.EnableBuild = enableBuild;
        return builder.GetPrebuild();
    }

    internal static FileInfo FindCacheFile(DirectoryInfo directoryInfo) => directoryInfo.GetFiles("*" + CacheFilenameSuffix).FirstOrDefault();

    internal const string CacheFilenameSuffix = ".interactive.workspaceData.cache";

    internal const string DirectoryBuildTargetFilename = "Directory.Build.targets";

    internal const string DirectoryBuildTargetsContent =
        """
        <Project>
          <Target Name="CollectProjectData" AfterTargets="Build">
            <ItemGroup>
              <ProjectData Include="ProjectGuid=$(ProjectGuid)">
                <Type>String</Type>
              </ProjectData>
              <ProjectData Include="%(ProjectReference.Identity)">
                <Prefix>ProjectReferences=</Prefix>
                <Type>Array</Type>
              </ProjectData>
              <ProjectData Include="ProjectFilePath=$(MSBuildProjectFullPath)">
                <Type>String</Type>
              </ProjectData>
              <ProjectData Include="LanguageName=C#">
                <Type>String</Type>
              </ProjectData>
              <ProjectData Include="PropertyTargetPath=$(TargetPath)">
                <Type>String</Type>
              </ProjectData>
              <ProjectData Include="%(Compile.FullPath)" Condition="!$([System.String]::new('%(Compile.Identity)').Contains('obj\'))">
                <Prefix>SourceFiles=</Prefix>
                <Type>Array</Type>
              </ProjectData>
              <ProjectData Include="%(ReferencePath.Identity)">
                <Prefix>References=</Prefix>
                <Type>Array</Type>
              </ProjectData>
              <ProjectData Include="%(Analyzer.Identity)">
                <Prefix>AnalyzerReferences=</Prefix>
                <Type>Array</Type>
              </ProjectData>
              <ProjectData Include="$(DefineConstants)">
                <Prefix>PreprocessorSymbols=</Prefix>
                <Type>String</Type>
              </ProjectData>
              <ProjectData Include="PropertyLangVersion=$(LangVersion)">
                <Type>String</Type>
              </ProjectData>
              <ProjectData Include="PropertyOutputType=$(OutputType)">
                <Type>String</Type>
              </ProjectData>
            </ItemGroup>
        
            <!-- Split PreprocessorSymbols into individual items -->
            <ItemGroup>
              <PreprocessorSymbolItems Include="$(DefineConstants.Split(';'))" />
            </ItemGroup>
        
            <!-- Transform the ProjectData and PreprocessorSymbolItems to include the prefix -->
            <ItemGroup>
              <ProjectDataLines Include="$([System.String]::Format('{0}{1}', %(ProjectData.Prefix), %(ProjectData.Identity)))" />
              <ProjectDataLines Include="$([System.String]::Format('PreprocessorSymbols={0}', %(PreprocessorSymbolItems.Identity)))" />
            </ItemGroup>
        
            <!-- Write collected project data to a file -->
            <WriteLinesToFile Lines="@(ProjectDataLines)"
                              File="$(MSBuildProjectFullPath).interactive.workspaceData.cache"
                              Overwrite="True"
                              WriteOnlyWhenDifferent="True" />
          </Target>
        </Project>
        """;

    internal static void CleanObjFolder(DirectoryInfo directoryInfo)
    {
        var targets = directoryInfo.GetDirectories("obj");
        foreach (var target in targets)
        {
            target.Delete(true);
        }
    }
}