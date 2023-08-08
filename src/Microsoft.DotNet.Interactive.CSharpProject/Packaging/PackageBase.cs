// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn;
using Microsoft.DotNet.Interactive.CSharpProject.RoslynWorkspaceUtilities;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging;

public abstract class PackageBase : 
    IHaveADirectory,
    ICanSupportWasm, 
    IHaveADirectoryAccessor
{
    IDirectoryAccessor IHaveADirectoryAccessor.Directory => new FileSystemDirectoryAccessor(Directory);

    internal const string FullBuildBinlogFileName = "package_fullBuild.binlog";

    private readonly AsyncLazy<bool> _lazyCreation;
    private bool? _canSupportBlazor;

    protected PackageBase(
        string name = null,
        IPackageInitializer initializer = null,
        DirectoryInfo directory = null)
    {
        Initializer = initializer;

        Name = name ?? directory?.Name ?? throw new ArgumentException($"You must specify {nameof(name)}, {nameof(directory)}, or both.");
        Directory = directory ?? new DirectoryInfo(Path.Combine(Package.DefaultPackagesDirectory.FullName, Name));

        _lazyCreation = new AsyncLazy<bool>(() => this.Create(Initializer));
        LastBuildErrorLogFile = new FileInfo(Path.Combine(Directory.FullName, ".net-interactive-builderror"));
    }

    public IPackageInitializer Initializer { get; protected set; }

    public string Name { get; }

    public DirectoryInfo Directory { get; set; }
       

    protected Task<bool> EnsureCreatedAsync() => _lazyCreation.ValueAsync();

    protected virtual async Task EnsureBuiltAsync([CallerMemberName] string caller = null)
    {
        {
            await EnsureCreatedAsync();

            await FullBuildAsync();
        }
    }

    public virtual async Task EnsureReadyAsync()
    {
        await EnsureCreatedAsync();

        await EnsureBuiltAsync();
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
            if (_canSupportBlazor == null)
            {
                _canSupportBlazor = Directory?.Parent?.GetDirectories($"runner-{Name}")?.Length == 1;
            }

            return _canSupportBlazor.Value;
        }
    }

    public virtual async Task FullBuildAsync()
    {
        await DotnetBuildAsync();
    }

    protected async Task DotnetBuildAsync()
    {
        {
            this.CleanObjFolder();
            var projectFile = this.GetProjectFile();

            string tempDirectoryBuildTargetsFile =
                Path.Combine(Path.GetDirectoryName(projectFile.FullName), BuildCacheFileUtilities.DirectoryBuildTargetFilename);
            File.WriteAllText(tempDirectoryBuildTargetsFile, BuildCacheFileUtilities.DirectoryBuildTargetsContent);

            var args = "";
            if (projectFile?.Exists == true)
            {
                args = $@"""{projectFile.FullName}"" {args}";
            }

            var result = await new Dotnet(Directory).Build(args: args);

            if (result.ExitCode != 0)
            {
                File.WriteAllText(
                    LastBuildErrorLogFile.FullName,
                    string.Join(Environment.NewLine, result.Error));
            }
            else if (LastBuildErrorLogFile.Exists)
            {
                LastBuildErrorLogFile.Delete();
            }

            // Clean up the temp project file
            File.Delete(tempDirectoryBuildTargetsFile);

            result.ThrowOnFailure();
        }
    }

    protected FileInfo LastBuildErrorLogFile { get; }
}