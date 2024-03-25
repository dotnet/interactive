// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;
using static Pocket.Logger<Microsoft.DotNet.Interactive.CSharpProject.Packaging.Package>;
using Disposable = System.Reactive.Disposables.Disposable;
using static Microsoft.DotNet.Interactive.CSharpProject.RoslynWorkspaceUtilities.RoslynWorkspaceUtilities;
using Microsoft.DotNet.Interactive.CSharpProject.RoslynWorkspaceUtilities;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging;

public class Package 
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _packageBuildSemaphores = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _packagePublishSemaphores = new();

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
    private bool? _canSupportWasm;
    private readonly IPackageInitializer _packageInitializer;

    private int buildCount = 0;
    private int publishCount = 0;

    private FileInfo _entryPointAssemblyPath;
    private static string _targetFramework;
    private readonly Logger _log;
    private readonly Subject<Unit> _fullBuildRequestChannel;

    protected CodeAnalysis.Workspace _roslynWorkspace;

    private readonly IScheduler _buildThrottleScheduler;
    private readonly SerialDisposable _fullBuildThrottlerSubscription;

    private readonly SemaphoreSlim _buildSemaphore;
    private readonly SemaphoreSlim _publishSemaphore;

    private readonly Subject<Unit> _designTimeBuildRequestChannel;
    private readonly SerialDisposable _designTimeBuildThrottlerSubscription;
    private BuildDataResults _designTimeBuildResult;

    private DateTimeOffset? _lastDesignTimeBuildTime;
    private DateTimeOffset? _lastSuccessfulBuildTime;
    private readonly FileInfo _lastBuildErrorLogFile;

    private TaskCompletionSource<CodeAnalysis.Workspace> _fullBuildCompletionSource = new();
    private TaskCompletionSource<CodeAnalysis.Workspace> _designTimeBuildCompletionSource = new();

    private readonly object _fullBuildCompletionSourceLock = new();
    private readonly object _designTimeBuildCompletionSourceLock = new();

    public Package(
        string name,
        IPackageInitializer initializer = null,
        DirectoryInfo directory = null,
        bool enableBuild = false)
    {
        Name = name;
        EnableBuild = enableBuild;
        Directory = directory ?? new DirectoryInfo(Path.Combine(DefaultPackagesDirectory.FullName, Name));
        _packageInitializer = initializer ?? new PackageInitializer("console", Name);

        _lazyCreation = new AsyncLazy<bool>(() => Create(_packageInitializer));
        _lastBuildErrorLogFile = new FileInfo(Path.Combine(Directory.FullName, ".net-interactive-builderror"));

        _log = new Logger($"{nameof(Package)}:{Name}");
        _buildThrottleScheduler = TaskPoolScheduler.Default;

        _fullBuildRequestChannel = new Subject<Unit>();
        _fullBuildThrottlerSubscription = new SerialDisposable();

        _designTimeBuildRequestChannel = new Subject<Unit>();
        _designTimeBuildThrottlerSubscription = new SerialDisposable();

        InitializeFullBuildChannel();
        InitializeDesignTimeBuildChannel();

        if (!TryLoadDesignTimeBuildFromCacheFile())
        {
            if (!EnableBuild)
            {
                throw new InvalidOperationException($"Prebuild package not found at {Directory} and build-on-demand is disabled.");
            }
        }

        // FIX: (Package) ever-growing collections
        _buildSemaphore = _packageBuildSemaphores.GetOrAdd(Name, _ => new SemaphoreSlim(1, 1));
        _publishSemaphore = _packagePublishSemaphores.GetOrAdd(Name, _ => new SemaphoreSlim(1, 1));
    }

    public bool CanSupportWasm
    {
        get
        {
            // The directory structure for the blazor packages is as follows
            // project |--> packTarget
            //         |--> runner-abc 
            // The packTarget is the project that contains this package
            //Hence the parent directory must be looked for the blazor runner
            if (_canSupportWasm is null)
            {
                _canSupportWasm = Directory?.Parent?.GetDirectories($"runner-{Name}").Length == 1;
            }

            return _canSupportWasm.Value;
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

    private bool TryLoadDesignTimeBuildFromCacheFile()
    {
        if (Directory.Exists)
        {
            var cacheFile = FindCacheFile(Directory);
            if (cacheFile is not null)
            {
                LoadDesignTimeBuildDataFromBuildCacheFile(cacheFile).Wait();
                return true;
            }
        }

        return false;
    }

    private async Task LoadDesignTimeBuildDataFromBuildCacheFile(FileSystemInfo cacheFile)
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
                throw new InvalidOperationException("The cache file seems to contain no solutions or projects");
            }

            _roslynWorkspace = null;
            _designTimeBuildResult = result;
            _lastDesignTimeBuildTime = cacheFile.LastWriteTimeUtc;

            if (result.Succeeded && !cacheFile.Name.EndsWith(BuildCacheFileUtilities.cacheFilenameSuffix))
            {
                _lastSuccessfulBuildTime = cacheFile.LastWriteTimeUtc;
                _roslynWorkspace = _designTimeBuildResult.Workspace;
            }
        }
    }

    private FileInfo GetProjectFile() => Directory.GetFiles("*.csproj").FirstOrDefault();

    public DateTimeOffset? PublicationTime { get; private set; }

    public static DirectoryInfo DefaultPackagesDirectory { get; }

    public FileInfo EntryPointAssemblyPath => 
        _entryPointAssemblyPath ??= this.GetEntryPointAssemblyPath();

    public string TargetFramework => 
        _targetFramework ??= this.GetTargetFramework();

    public Task<CodeAnalysis.Workspace> CreateWorkspaceForRunAsync()
    {
        CreateCompletionSourceIfNeeded(ref _fullBuildCompletionSource, _fullBuildCompletionSourceLock);

        _fullBuildRequestChannel.OnNext(Unit.Default);

        return _fullBuildCompletionSource.Task;
    }

    public Task<CodeAnalysis.Workspace> CreateWorkspaceForLanguageServicesAsync()
    {
        var shouldBuild = ShouldDoDesignTimeBuild();

        if (!shouldBuild)
        {
            var ws = _roslynWorkspace ?? CreateRoslynWorkspace();
            if (ws is not null)
            {
                return Task.FromResult(ws);
            }
        }

        CreateCompletionSourceIfNeeded(ref _designTimeBuildCompletionSource, _designTimeBuildCompletionSourceLock);

        _designTimeBuildRequestChannel.OnNext(Unit.Default);

        return _designTimeBuildCompletionSource.Task;
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

    private void InitializeFullBuildChannel()
    {
        _fullBuildThrottlerSubscription.Disposable = _fullBuildRequestChannel
            .Throttle(TimeSpan.FromSeconds(0.5), _buildThrottleScheduler)
            .ObserveOn(TaskPoolScheduler.Default)
            .Subscribe(
                async (_) =>
                {
                    try
                    {
                        await ProcessFullBuildRequest();
                    }
                    catch (Exception e)
                    {
                        SetCompletionSourceException(_fullBuildCompletionSource, e, _fullBuildCompletionSourceLock);
                    }
                },
                error =>
                {
                    SetCompletionSourceException(_fullBuildCompletionSource, error, _fullBuildCompletionSourceLock);
                    InitializeFullBuildChannel();
                });
    }

    private void InitializeDesignTimeBuildChannel()
    {
        _designTimeBuildThrottlerSubscription.Disposable = _designTimeBuildRequestChannel
            .Throttle(TimeSpan.FromSeconds(0.5), _buildThrottleScheduler)
            .ObserveOn(TaskPoolScheduler.Default)
            .Subscribe(
                async (_) =>
                {
                    try
                    {
                        await ProcessDesignTimeBuildRequest();
                    }
                    catch (Exception e)
                    {
                        SetCompletionSourceException(_designTimeBuildCompletionSource, e, _designTimeBuildCompletionSourceLock);
                    }
                },
                error =>
                {
                    SetCompletionSourceException(_designTimeBuildCompletionSource, error, _designTimeBuildCompletionSourceLock);
                    InitializeDesignTimeBuildChannel();
                });
    }

    private async Task ProcessFullBuildRequest()
    {
        await EnsureCreatedAsync();
        await EnsureBuiltAsync();
        var ws = CreateRoslynWorkspace();
        SetCompletionSourceResult(_fullBuildCompletionSource, ws, _fullBuildCompletionSourceLock);
    }

    private async Task ProcessDesignTimeBuildRequest()
    {
        await EnsureCreatedAsync();
        await EnsureDesignTimeBuilt();
        var ws = CreateRoslynWorkspace();
        SetCompletionSourceResult(_designTimeBuildCompletionSource, ws, _designTimeBuildCompletionSourceLock);
    }

    private CodeAnalysis.Workspace CreateRoslynWorkspace()
    {
        var build = _designTimeBuildResult;

        if (build is null)
        {
            throw new InvalidOperationException("No design time or full build available");
        }

        var ws = build.Workspace;

        if (!ws.CanBeUsedToGenerateCompilation())
        {
            _roslynWorkspace = null;
            _designTimeBuildResult = null;
            _lastDesignTimeBuildTime = null;
            throw new InvalidOperationException("The roslyn workspace cannot be used to generate a compilation");
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
        await EnsureCreatedAsync();

        await EnsureBuiltAsync();
    }

    protected async Task EnsureBuiltAsync([CallerMemberName] string caller = null)
    {
        using var operation = _log.OnEnterAndConfirmOnExit();

        await EnsureCreatedAsync();

        if (IsFullBuildNeeded())
        {
            await DoFullBuildAsync();
        }
        else
        {
            operation.Info("Workspace already built");
        }

        operation.Succeed();
    }

    protected async Task EnsureDesignTimeBuilt([CallerMemberName] string caller = null)
    {
        using var operation = _log.OnEnterAndConfirmOnExit();

        await EnsureCreatedAsync();

        if (ShouldDoDesignTimeBuild())
        {
            await DoDesignTimeBuildAsync();
        }
        else
        {
            operation.Info("Workspace already built");
        }

        operation.Succeed();
    }

    public virtual async Task EnsurePublishedAsync()
    {
        await EnsureBuiltAsync();
            
        using var operation = _log.OnEnterAndConfirmOnExit();
            
        if (PublicationTime == null || PublicationTime < _lastSuccessfulBuildTime)
        {
            await Publish();
        }

        operation.Succeed();
    }
    
    public async Task DoFullBuildAsync()
    {
        if (!EnableBuild)
        {
            // FIX: (DoFullBuildAsync) 
            throw new InvalidOperationException($"Full build is disabled for package {Name}");
        }

        using var operation = Log.OnEnterAndConfirmOnExit();

        try
        {
            operation.Info("Building package {name}", Name);

            // When a build finishes, buildCount is reset to 0. If, when we increment
            // the value, we get a value > 1, someone else has already started another
            // build
            var buildInProgress = Interlocked.Increment(ref buildCount) > 1;

            await _buildSemaphore.WaitAsync();

            using (Disposable.Create(() => _buildSemaphore.Release()))
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
        await LoadDesignTimeBuildDataFromBuildCacheFile(cacheFile);

        Interlocked.Exchange(ref buildCount, 0);
    }

    protected async Task DotnetBuildAsync()
    {
        BuildCacheFileUtilities.CleanObjFolder(Directory);

        var projectFile = this.GetProjectFile();

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

    protected async Task Publish()
    {
        using (var operation = _log.OnEnterAndConfirmOnExit())
        {
            operation.Info("Publishing package {name}", Name);
            var publishInProgress = Interlocked.Increment(ref publishCount) > 1;
            await _publishSemaphore.WaitAsync();

            if (publishInProgress)
            {
                operation.Info("Skipping publish for package {name}", Name);
                return;
            }

            CommandLineResult result;
            using (Disposable.Create(() => _publishSemaphore.Release()))
            {
                operation.Info("Publishing workspace in {directory}", Directory);
                result = await new Dotnet(Directory)
                    .Publish("--no-dependencies --no-restore --no-build");
            }

            result.ThrowOnFailure();

            operation.Info("Workspace published");
            operation.Succeed();
            PublicationTime = DateTimeOffset.Now;
            Interlocked.Exchange(ref publishCount, 0);
        }
    }

    public override string ToString()
    {
        return $"{Name} ({Directory.FullName})";
    }

    public Task<CodeAnalysis.Workspace> CreateWorkspaceAsync()
    {
        return CreateWorkspaceForRunAsync();
    }

    protected SyntaxTree CreateInstrumentationEmitterSyntaxTree()
    {
        var resourceName = "WorkspaceServer.Servers.Roslyn.Instrumentation.InstrumentationEmitter.cs";

        var assembly = typeof(PackageExtensions).Assembly;

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        using (var reader = new StreamReader(stream ?? throw new InvalidOperationException($"Resource \"{resourceName}\" not found"), Encoding.UTF8))
        {
            var source = reader.ReadToEnd();

            var parseOptions = _designTimeBuildResult.CSharpParseOptions;
            var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(source), parseOptions);

            return syntaxTree;
        }
    }

    protected virtual bool IsFullBuildNeeded()
    {
        return _roslynWorkspace is not null;
    }

    protected virtual bool ShouldDoDesignTimeBuild() =>
        _designTimeBuildResult is null || 
        _designTimeBuildResult.Succeeded == false;

    private protected async Task<BuildDataResults> DoDesignTimeBuildAsync()
    {
        using var operation = _log.OnEnterAndConfirmOnExit();

        BuildDataResults result;
        var csProj = GetProjectFile();
        var logWriter = new StringWriter();

        using (await FileLock.TryCreateAsync(Directory))
        {
            await BuildCacheFileUtilities.BuildAndCreateCacheFileAsync(csProj.FullName);
            result = ResultsFromCacheFileUsingProjectFilePath(csProj.FullName);
        }

        _designTimeBuildResult = result;
        _lastDesignTimeBuildTime = DateTimeOffset.Now;

        if (result?.Succeeded == false)
        {
            var logData = logWriter.ToString();
            File.WriteAllText(
                _lastBuildErrorLogFile.FullName,
                string.Join(Environment.NewLine, "Design Time Build Error", logData));
        }
        else if (_lastBuildErrorLogFile.Exists)
        {
            _lastBuildErrorLogFile.Delete();
        }

        operation.Succeed();

        return result;
    }

    internal virtual SyntaxTree GetInstrumentationEmitterSyntaxTree() =>
        CreateInstrumentationEmitterSyntaxTree();

    public async Task<bool> Create(IPackageInitializer initializer)
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

    public static async Task<Package> GetOrCreateConsolePackageAsync(bool enableBuild)
    {
        var packageBuilder = new PackageBuilder("console");
        packageBuilder.UseTemplate("console");
        packageBuilder.UseLanguageVersion("latest");
        packageBuilder.AddPackageReference("Newtonsoft.Json", "13.0.1");
        packageBuilder.EnableBuild = enableBuild;
        var package = packageBuilder.GetPackage();

        if (package.EnableBuild)
        {
            await package.CreateWorkspaceForRunAsync();
        }
        else
        {
            // FIX: (GetOrCreateConsolePackageAsync) keep immutable instance in memory
        }
      
        return package;
    }

    internal static FileInfo FindCacheFile(DirectoryInfo directoryInfo) => directoryInfo.GetFiles("*" + BuildCacheFileUtilities.cacheFilenameSuffix).FirstOrDefault();
}