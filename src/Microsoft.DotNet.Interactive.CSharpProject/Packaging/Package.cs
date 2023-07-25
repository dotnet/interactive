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
using System.Xml.Linq;
using System.Xml.XPath;
using Buildalyzer;
using Buildalyzer.Workspaces;
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
using Microsoft.DotNet.Interactive.CSharpProject.MSBuildTools;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging;

public abstract class Package :
    PackageBase,
    ICreateWorkspaceForLanguageServices,
    ICreateWorkspaceForRun
{
    internal const string DesignTimeBuildBinlogFileName = "package_designTimeBuild.binlog";


    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _packageBuildSemaphores = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _packagePublishSemaphores = new();

    static Package()
    {
        const string workspacesPathEnvironmentVariableName = "TRYDOTNET_PACKAGES_PATH";

        var environmentVariable = Environment.GetEnvironmentVariable(workspacesPathEnvironmentVariableName);

        DefaultPackagesDirectory =
            environmentVariable != null
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

        Log.Info("Packages path is {DefaultWorkspacesDirectory}", DefaultPackagesDirectory);
    }

    private int buildCount = 0;
    private int publishCount = 0;

    private bool? _isWebProject;
    private bool? _isUnitTestProject;
    private FileInfo _entryPointAssemblyPath;
    private static string _targetFramework;
    private readonly Logger _log;
    private readonly Subject<Unit> _fullBuildRequestChannel;

    private readonly IScheduler _buildThrottleScheduler;
    private readonly SerialDisposable _fullBuildThrottlerSubscription;

    private readonly SemaphoreSlim _buildSemaphore;
    private readonly SemaphoreSlim _publishSemaphore;

    private readonly Subject<Unit> _designTimeBuildRequestChannel;
    private readonly SerialDisposable _designTimeBuildThrottlerSubscription;

    private TaskCompletionSource<CodeAnalysis.Workspace> _fullBuildCompletionSource = new();
    private TaskCompletionSource<CodeAnalysis.Workspace> _designTimeBuildCompletionSource = new();

    private readonly object _fullBuildCompletionSourceLock = new();
    private readonly object _designTimeBuildCompletionSourceLock = new();

    protected Package(
        string name = null,
        IPackageInitializer initializer = null,
        DirectoryInfo directory = null,
        IScheduler buildThrottleScheduler = null) : base(name, initializer, directory)
    {
        Initializer = initializer ?? new PackageInitializer("console", Name);

        _log = new Logger($"{nameof(Package)}:{Name}");
        _buildThrottleScheduler = buildThrottleScheduler ?? TaskPoolScheduler.Default;

        _fullBuildRequestChannel = new Subject<Unit>();
        _fullBuildThrottlerSubscription = new SerialDisposable();

        _designTimeBuildRequestChannel = new Subject<Unit>();
        _designTimeBuildThrottlerSubscription = new SerialDisposable();

        SetupWorkspaceCreationFromBuildChannel();
        SetupWorkspaceCreationFromDesignTimeBuildChannel();
        TryLoadDesignTimeBuildFromBuildLog();

        _buildSemaphore = _packageBuildSemaphores.GetOrAdd(Name, _ => new SemaphoreSlim(1, 1));
        _publishSemaphore = _packagePublishSemaphores.GetOrAdd(Name, _ => new SemaphoreSlim(1, 1));
        RoslynWorkspace = null;
    }

    private void TryLoadDesignTimeBuildFromBuildLog()
    {
        if (Directory.Exists)
        {
            var binLog = this.FindLatestBinLog();
            if (binLog != null)
            {
                LoadDesignTimeBuildFromBuildLogFile(this, binLog).Wait();
            }
        }
    }
    private static async Task LoadDesignTimeBuildFromBuildLogFile(Package package, FileSystemInfo binLog)
    {
        var projectFile = package.GetProjectFile();
        if (projectFile != null &&
            binLog.LastWriteTimeUtc >= projectFile.LastWriteTimeUtc)
        {
            MyAnalyzerResult result;
            using (await FileLock.TryCreateAsync(package.Directory))
            {
                var manager = new MyBuildalyzer();
                result = manager.Analyze(projectFile.FullName);
            }

            if (result is null)
            {
                throw new InvalidOperationException("The build log seems to contain no solutions or projects");
            }

            package.RoslynWorkspace = null;
            package.DesignTimeBuildResult = result;
            package.LastDesignTimeBuild = binLog.LastWriteTimeUtc;
            if (result.Succeeded && !binLog.Name.EndsWith(DesignTimeBuildBinlogFileName))
            {
                package.LastSuccessfulBuildTime = binLog.LastWriteTimeUtc;
                if (package.MyDesignTimeBuildResult.TryGetWorkspace(out var ws))
                {
                    package.RoslynWorkspace = ws;
                }
            }
        }
    }

      

    private DateTimeOffset? LastDesignTimeBuild { get; set; }

    private DateTimeOffset? LastSuccessfulBuildTime { get; set; }

    public DateTimeOffset? PublicationTime { get; private set; }

    public bool IsUnitTestProject =>
        _isUnitTestProject ??
        (_isUnitTestProject = Directory.GetFiles("*.testadapter.dll", SearchOption.AllDirectories).Any()).Value;

    public bool IsWebProject
    {
        get
        {
            if (_isWebProject == null && this.GetProjectFile() is FileInfo csproj)
            {
                var csprojXml = File.ReadAllText(csproj.FullName);

                var xml = XElement.Parse(csprojXml);

                var isAspNetCore2 = xml.XPathSelectElement("//ItemGroup/PackageReference[@Include='Microsoft.AspNetCore.App']") != null;

                var isAspNetCore3 = xml.DescendantsAndSelf()
                    .FirstOrDefault(n => n.Name == "Project")
                    ?.Attribute("Sdk")
                    ?.Value == "Microsoft.NET.Sdk.Web";

                _isWebProject = isAspNetCore2 || isAspNetCore3;
            }

            return _isWebProject ?? false;
        }
    }

    public static DirectoryInfo DefaultPackagesDirectory { get; }

    public FileInfo EntryPointAssemblyPath => 
        _entryPointAssemblyPath ??= this.GetEntryPointAssemblyPath(IsWebProject);

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
            var ws = RoslynWorkspace ?? CreateRoslynWorkspace();
            if (ws != null)
            {
                return Task.FromResult(ws);
            }
        }

        return RequestDesignTimeBuildAsync();
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

    private Task<CodeAnalysis.Workspace> RequestDesignTimeBuildAsync()
    {
        CreateCompletionSourceIfNeeded(ref _designTimeBuildCompletionSource, _designTimeBuildCompletionSourceLock);

        _designTimeBuildRequestChannel.OnNext(Unit.Default);
        return _designTimeBuildCompletionSource.Task;
    }

    private void SetupWorkspaceCreationFromBuildChannel()
    {
        _fullBuildThrottlerSubscription.Disposable = _fullBuildRequestChannel
            .Throttle(TimeSpan.FromSeconds(0.5), _buildThrottleScheduler)
            .ObserveOn(TaskPoolScheduler.Default)
            .Subscribe(
                async (budget) =>
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
                    SetupWorkspaceCreationFromBuildChannel();
                });
    }

    private void SetupWorkspaceCreationFromDesignTimeBuildChannel()
    {
        _designTimeBuildThrottlerSubscription.Disposable = _designTimeBuildRequestChannel
            .Throttle(TimeSpan.FromSeconds(0.5), _buildThrottleScheduler)
            .ObserveOn(TaskPoolScheduler.Default)
            .Subscribe(
                async (budget) =>
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
                    SetupWorkspaceCreationFromDesignTimeBuildChannel();
                });
    }

    private async Task ProcessFullBuildRequest()
    {
        await EnsureCreatedAsync();
        await EnsureBuiltAsync();
        var ws = CreateRoslynWorkspace();
        if (IsWebProject)
        {
            await EnsurePublishedAsync();
        }
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
        var build = DesignTimeBuildResult;
        if (build == null)
        {
            throw new InvalidOperationException("No design time or full build available");
        }

        var ws = build.RoslynWorkspace;

        if (!ws.CanBeUsedToGenerateCompilation())
        {
            RoslynWorkspace = null;
            DesignTimeBuildResult = null;
            LastDesignTimeBuild = null;
            throw new InvalidOperationException("The roslyn workspace cannot be used to generate a compilation");
        }

        var projectId = ws.CurrentSolution.ProjectIds.FirstOrDefault();
        var references = build.References;
        var metadataReferences = references.GetMetadataReferences();
        var solution = ws.CurrentSolution;
        solution = solution.WithProjectMetadataReferences(projectId, metadataReferences);
        ws.TryApplyChanges(solution);
        RoslynWorkspace = ws;
        return ws;
    }

    protected CodeAnalysis.Workspace RoslynWorkspace { get; set; }

    public override async Task EnsureReadyAsync()
    {
        await base.EnsureReadyAsync();

        if (RequiresPublish)
        {
            await EnsurePublishedAsync();
        }
    }

    protected override async Task EnsureBuiltAsync([CallerMemberName] string caller = null)
    {
        using (var operation = _log.OnEnterAndConfirmOnExit())
        {
            await EnsureCreatedAsync();

            if (ShouldDoFullBuild())
            {
                await FullBuildAsync();
            }
            else
            {
                operation.Info("Workspace already built");
            }

            operation.Succeed();
        }
    }

    protected async Task EnsureDesignTimeBuilt([CallerMemberName] string caller = null)
    {
        await EnsureCreatedAsync();
        using (var operation = _log.OnEnterAndConfirmOnExit())
        {
            if (ShouldDoDesignTimeBuild())
            {
                await DesignTimeBuild();
            }
            else
            {
                operation.Info("Workspace already built");
            }

            operation.Succeed();
        }
    }

    public virtual async Task EnsurePublishedAsync()
    {
        await EnsureBuiltAsync();
            
        using var operation = _log.OnEnterAndConfirmOnExit();
            
        if (PublicationTime == null || PublicationTime < LastSuccessfulBuildTime)
        {
            await Publish();
        }

        operation.Succeed();
    }

    public bool RequiresPublish => IsWebProject;

    public override async Task FullBuildAsync()
    {
        using (var operation = Log.OnEnterAndConfirmOnExit())
        {
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

            var binLog = this.FindLatestBinLog();
            await binLog.WaitForFileAvailable();
            await LoadDesignTimeBuildFromBuildLogFile(this, binLog);

            Interlocked.Exchange(ref buildCount, 0);
        }
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

            var parseOptions = DesignTimeBuildResult.GetCSharpParseOptions();
            var syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(source), parseOptions);

            return syntaxTree;
        }
    }

    protected MyAnalyzerResult DesignTimeBuildResult { get; set; }

    protected MyAnalyzerResult MyDesignTimeBuildResult { get; set; }

    protected virtual bool ShouldDoFullBuild()
    {
        return LastSuccessfulBuildTime == null
               || ShouldDoDesignTimeBuild()
               || (LastDesignTimeBuild > LastSuccessfulBuildTime);
    }

    protected virtual bool ShouldDoDesignTimeBuild()
    {
        return DesignTimeBuildResult == null
               || DesignTimeBuildResult.Succeeded == false;
    }

    protected async Task<MyAnalyzerResult> DesignTimeBuild()
    {
        using (var operation = _log.OnEnterAndConfirmOnExit())
        {
            MyAnalyzerResult result;
            var csProj = this.GetProjectFile();
            var logWriter = new StringWriter();

            using (await FileLock.TryCreateAsync(Directory))
            {
                var manager = new MyBuildalyzer();
                result = manager.Analyze(csProj.FullName);
            }

            DesignTimeBuildResult = result;
            LastDesignTimeBuild = DateTimeOffset.Now;
            if (result.Succeeded == false)
            {
                var logData = logWriter.ToString();
                File.WriteAllText(
                    LastBuildErrorLogFile.FullName,
                    string.Join(Environment.NewLine, "Design Time Build Error", logData));
            }
            else if (LastBuildErrorLogFile.Exists)
            {
                LastBuildErrorLogFile.Delete();
            }

            operation.Succeed();

            return result;
        }
    }

    public virtual SyntaxTree GetInstrumentationEmitterSyntaxTree() =>
        CreateInstrumentationEmitterSyntaxTree();
}