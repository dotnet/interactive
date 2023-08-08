// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging;

public class RebuildablePackage : Package
{
    private FileSystemWatcher _fileSystemWatcher;

    public RebuildablePackage(string name = null, IPackageInitializer initializer = null, DirectoryInfo directory = null, IScheduler buildThrottleScheduler = null) 
        : base(name, initializer, directory, buildThrottleScheduler)
    {
        if (BuildResultNeedsUpdate())
        {
            Reset();
        }
    }

    void SetupFileWatching()
    {
        if (_fileSystemWatcher == null)
        {
            _fileSystemWatcher = new FileSystemWatcher(Directory.FullName)
            {
                EnableRaisingEvents = true
            };

            _fileSystemWatcher.Changed += FileSystemWatcherOnChangedOrDeleted;
            _fileSystemWatcher.Deleted += FileSystemWatcherOnDeleted;
            _fileSystemWatcher.Renamed += FileSystemWatcherOnRenamed;
            _fileSystemWatcher.Created += FileSystemWatcherOnCreated;
        }
    }

    protected override async Task EnsureBuiltAsync([CallerMemberName] string caller = null)
    {
        await base.EnsureBuiltAsync(caller);
        SetupFileWatching();
    }

    public override async Task EnsureReadyAsync()
    {
        await base.EnsureReadyAsync();
        SetupFileWatching();
    }

    private static bool IsProjectFile(string fileName)
    {
        return fileName.EndsWith(".csproj", StringComparison.InvariantCultureIgnoreCase)
               || fileName.EndsWith(".fsproj", StringComparison.InvariantCultureIgnoreCase);
    }

    private static bool IsCodeFile(string fileName)
    {
        return fileName.EndsWith(".cs", StringComparison.InvariantCultureIgnoreCase)
               || fileName.EndsWith(".fs", StringComparison.InvariantCultureIgnoreCase);
    }

    private static bool IsBuildLogFile(string fileName)
    {
        return fileName.EndsWith(".binlog", StringComparison.InvariantCultureIgnoreCase);
    }

    private void FileSystemWatcherOnDeleted(object sender, FileSystemEventArgs e)
    {
        HandleFileChanges(e.Name);
    }

    private void FileSystemWatcherOnCreated(object sender, FileSystemEventArgs e)
    {
        var fileName = e.Name;
        if (IsProjectFile(fileName) || IsCodeFile(fileName))
        {
            Reset();
        }
    }

    private void FileSystemWatcherOnRenamed(object sender, RenamedEventArgs e)
    {
        HandleFileChanges(e.OldName);
    }

    private void HandleFileChanges(string fileName)
    {
        var build = DesignTimeBuildResult;
        if (build == null)
        {
            return;
        }

        if (IsProjectFile(fileName) || IsBuildLogFile(fileName))
        {
            Reset();
        }
        else if (IsCodeFile(fileName))
        {
            Reset();
        }
    }

    private void Reset()
    {
        DesignTimeBuildResult = null;
        RoslynWorkspace = null;
    }

    private void FileSystemWatcherOnChangedOrDeleted(object sender, FileSystemEventArgs e)
    {
        HandleFileChanges(e.Name);
    }

    private bool BuildResultNeedsUpdate()
    {
        if (DesignTimeBuildResult != null)
        {
            var filesInDirectory = Directory.GetFiles("*.cs", SearchOption.AllDirectories).Select(file => file.FullName);
            var documentsInLastBuild = DesignTimeBuildResult.Workspace.CurrentSolution.Projects.First().Documents.Select(document => document.FilePath);

            return filesInDirectory.Count() != documentsInLastBuild.Count() || !documentsInLastBuild.All(filesInDirectory.Contains);
        }

        return true;
    }
}