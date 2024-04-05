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
using static Microsoft.DotNet.Interactive.CSharpProject.RoslynWorkspaceUtilities.RoslynWorkspaceUtilities;
using static Pocket.Logger<Microsoft.DotNet.Interactive.CSharpProject.Build.Package>;
using Disposable = System.Reactive.Disposables.Disposable;

namespace Microsoft.DotNet.Interactive.CSharpProject.Build;

public class Package 
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _buildSemaphores = new();

    static Package()
    {
        const string workspacesPathEnvironmentVariableName = "TRYDOTNET_PACKAGES_PATH";

        var environmentVariable = Environment.GetEnvironmentVariable(workspacesPathEnvironmentVariableName);

        DefaultPackagesDirectory =
            environmentVariable is not null
                ? new DirectoryInfo(environmentVariable)
                : new DirectoryInfo(
                    Path.Combine(
                        Paths.UserProfile,
                        ".trydotnet",
                        "packages"));

        if (!DefaultPackagesDirectory.Exists)
        {
            DefaultPackagesDirectory.Create();
        }

        Log.Info("Prebuild packages path is {DefaultWorkspacesDirectory}", DefaultPackagesDirectory);
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

    public Package(
        string name,
        IPackageInitializer initializer = null,
        DirectoryInfo directory = null,
        bool enableBuild = false)
    {
        Name = name;
        EnableBuild = enableBuild;
        Directory = directory ?? new DirectoryInfo(Path.Combine(DefaultPackagesDirectory.FullName, Name));
        var packageInitializer = initializer ?? new PackageInitializer("console", Name);

        _lazyCreation = new AsyncLazy<bool>(() => CreatePackage(packageInitializer));
        _lastBuildErrorLogFile = new FileInfo(Path.Combine(Directory.FullName, ".net-interactive-builderror"));

        _log = new Logger($"{nameof(Package)}:{Name}");
        _buildThrottleScheduler = TaskPoolScheduler.Default;

        _buildRequestChannel = new Subject<Unit>();
        _buildThrottleSubscription = new SerialDisposable();
        
        InitializeBuildChannel();

        if (!TryLoadWorkspaceFromCache())
        {
            if (!EnableBuild)
            {
                throw new InvalidOperationException($"Prebuild package not found at {Directory} and build-on-demand is disabled.");
            }
        }
    }

    public bool EnableBuild { get; }

    public DirectoryInfo Directory { get; }

    public string Name { get; }

    private Task<bool> EnsureCreatedAsync()
    {
        if (!EnableBuild)
        {
            return Task.FromResult(false);
        }

        return _lazyCreation.ValueAsync();
    }

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
    
    public static DirectoryInfo DefaultPackagesDirectory { get; }

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
            Log.Info("Workspace already loaded for package {name}.", Name);
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
            operation.Info("Workspace already built");
        }

        operation.Succeed();
    }

    public async Task BuildAsync()
    {
        if (!EnableBuild)
        {
            throw new InvalidOperationException($"Full build is disabled for package {this}");
        }

        using var operation = Log.OnEnterAndConfirmOnExit();

        var buildSemaphore = _buildSemaphores.GetOrAdd(Name, _ => new SemaphoreSlim(1, 1));

        try
        {
            await EnsureCreatedAsync();

            operation.Info("Building package {name}", Name);

            // When a build finishes, buildCount is reset to 0. If, when we increment
            // the value, we get a value > 1, someone else has already started another
            // build
            var buildInProgress = Interlocked.Increment(ref buildCount) > 1;

            await buildSemaphore.WaitAsync();

            using (Disposable.Create(() => buildSemaphore.Release()))
            {
                if (buildInProgress)
                {
                    operation.Info("Skipping build for package {name}", Name);
                    return;
                }

                using (await FileLock.TryCreateAsync(Directory))
                {
                    await DotnetBuildAsync();
                }
            }

            operation.Info("Workspace built");

            operation.Succeed();
        }
        catch (Exception exception)
        {
            operation.Error("Exception building workspace", exception);
        }

        var cacheFile = FindCacheFile(Directory);

        await cacheFile.WaitForFileAvailableAsync();
        await LoadRoslynWorkspaceFromCache(cacheFile);

        Interlocked.Exchange(ref buildCount, 0);
    }

    private async Task DotnetBuildAsync()
    {
        BuildCacheFileUtilities.CleanObjFolder(Directory);

        var projectFile = GetProjectFile();

        string tempDirectoryBuildTargetsFile =
            Path.Combine(Path.GetDirectoryName(projectFile.FullName), BuildCacheFileUtilities.DirectoryBuildTargetFilename);

        await File.WriteAllTextAsync(tempDirectoryBuildTargetsFile, BuildCacheFileUtilities.DirectoryBuildTargetsContent);

        var args = "";
        if (projectFile.Exists)
        {
            args = $"""
                "{projectFile.FullName}" {args}
                """;
        }

        var result = await new Dotnet(Directory).Build(args: args);

        if (result.ExitCode != 0)
        {
            await File.WriteAllTextAsync(
                _lastBuildErrorLogFile.FullName,
                string.Join(Environment.NewLine, result.Error));
        }
        else if (_lastBuildErrorLogFile.Exists)
        {
            _lastBuildErrorLogFile.Delete();
        }

        // Clean up the temp project file
        File.Delete(tempDirectoryBuildTargetsFile);

        result.ThrowOnFailure();
    }
    
    public override string ToString() => $"{Name} ({Directory.FullName})";

    protected virtual bool IsBuildNeeded() => _roslynWorkspace is null;
    
    public async Task<bool> CreatePackage(IPackageInitializer initializer)
    {
        using var operation = Log.OnEnterAndConfirmOnExit();

        if (!Directory.Exists)
        {
            operation.Info("Creating directory {directory}", Directory);
            Directory.Create();
            Directory.Refresh();
        }

        using (await FileLock.TryCreateAsync(Directory))
        {
            if (!Directory.GetFiles("*", SearchOption.AllDirectories).Where(f => !FileLock.IsLockFile(f)).Any())
            {
                operation.Info("Initializing package using {_initializer} in {directory}", initializer,
                               Directory);
                await initializer.InitializeAsync(Directory);
            }
        }

        operation.Succeed();

        return true;
    }

    public static async Task<Package> GetOrCreateConsolePackageAsync(bool enableBuild = false)
    {
        var packageBuilder = new PackageBuilder("console");
        packageBuilder.UseTemplate("console");
        packageBuilder.UseLanguageVersion("latest");
        packageBuilder.AddPackageReference("Newtonsoft.Json", "13.0.1");
        packageBuilder.EnableBuild = enableBuild;
        var package = packageBuilder.GetPackage();
        return package;
    }

    internal static FileInfo FindCacheFile(DirectoryInfo directoryInfo) => directoryInfo.GetFiles("*" + BuildCacheFileUtilities.cacheFilenameSuffix).FirstOrDefault();
}