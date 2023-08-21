// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions.Execution;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Utility;

public class Dotnet
{
    protected readonly DirectoryInfo _workingDirectory;

    public Dotnet(DirectoryInfo workingDirectory = null)
    {
        _workingDirectory = workingDirectory ??
                            new DirectoryInfo(Directory.GetCurrentDirectory());
    }

    public async Task<CommandLineResult> New(string templateName, string args = null)
    {
        if (string.IsNullOrWhiteSpace(templateName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(templateName));
        }

        return await Execute($@"new ""{templateName}"" {args}");
    }

    public async Task<AddPackageResult> AddPackage(string packageId, string version = null)
    {
        var versionArg = string.IsNullOrWhiteSpace(version)
            ? ""
            : $"--version {version}";
        var executionResult = await Execute($"add package {versionArg} {packageId}");
        return new AddPackageResult(executionResult.ExitCode, executionResult.Output, executionResult.Error);
    }

    public Task<CommandLineResult> AddReference(FileInfo projectToReference, TimeSpan? timeout = null)
    {
        return Execute($@"add reference ""{projectToReference.FullName}""", timeout);
    }

    public Task<CommandLineResult> Build(string args = null, TimeSpan? timeout = null) =>
        Execute("build".AppendArgs(args), timeout);

    public Task<CommandLineResult> Clean(TimeSpan? timeout = null) =>
        Execute("clean", timeout);

    public Task<CommandLineResult> Execute(string args, TimeSpan? timeout = null) =>
        CommandLine.Execute(
            Path,
            args,
            _workingDirectory,
            timeout);

    public Process StartProcess(string args, Action<string> output = null, Action<string> error = null) =>
        CommandLine.StartProcess(
            Path.FullName,
            args,
            _workingDirectory,
            output,
            error);

    public Task<CommandLineResult> Publish(string args = null, TimeSpan? timeout = null) =>
        Execute("publish".AppendArgs(args), timeout);

    public Task<CommandLineResult> VSTest(string args) =>
        Execute("vstest".AppendArgs(args));

    public Task<CommandLineResult> ToolInstall(
        string packageName,
        DirectoryInfo toolPath = null,
        string addSource = null,
        string version = null)
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(packageName));
        }

        var versionArg = version is not null ? $"--version {version}" : "";
            
        var args = $@"{packageName}";
        if (toolPath is not null)
        {
            args += $@" --tool-path ""{toolPath.FullName.TrimTrailingSeparators()}""";
        }
        else
        {
            args += " --global";
        }
        args += $@" {versionArg}";

        if (addSource is not null)
        {
            args += $@" --add-source ""{addSource}""";
        }

        return Execute("tool install".AppendArgs(args));
    }

    public async Task<IEnumerable<string>> ToolList(DirectoryInfo directory = null)
    {
        var args = "tool list";
        if (directory is not null)
        {
            args += $@" --tool-path ""{directory.FullName}""";
        }
        else
        {
            args += " --global";
        }
        var result = await Execute(args);
        if (result.ExitCode != 0)
        {
            return Enumerable.Empty<string>();
        }

        // Output of dotnet tool list is:
        // Package Id        Version      Commands
        // -------------------------------------------
        // dotnettry.p1      1.0.0        dotnettry.p1

        string[] separator = new[] { " " };
        return result.Output
            .Skip(2)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Split(separator, StringSplitOptions.RemoveEmptyEntries)[2]);
    }

    public Task<CommandLineResult> Pack(string args = null, TimeSpan? timeout = null) =>
        Execute("pack".AppendArgs(args), timeout);

    private static readonly Lazy<FileInfo> _getPath = new Lazy<FileInfo>(() =>
        FindDotnetFromAppContext() ??
        FindDotnetFromPath());

    public static FileInfo Path => _getPath.Value;

    private static FileInfo FindDotnetFromPath()
    {
        FileInfo fileInfo = null;

        using (var process = Process.Start("dotnet"))
        {
            if (process is not null)
            {
                fileInfo = new FileInfo(process.MainModule.FileName);
            }
        }

        return fileInfo;
    }

    private static FileInfo FindDotnetFromAppContext()
    {
        var muxerFileName = "dotnet".ExecutableName();

        var fxDepsFile = GetDataFromAppDomain("FX_DEPS_FILE");

        if (!string.IsNullOrEmpty(fxDepsFile))
        {
            var muxerDir = new FileInfo(fxDepsFile).Directory?.Parent?.Parent?.Parent;

            if (muxerDir is not null)
            {
                var muxerCandidate = new FileInfo(System.IO.Path.Combine(muxerDir.FullName, muxerFileName));

                if (muxerCandidate.Exists)
                {
                    return muxerCandidate;
                }
            }
        }

        return null;
    }

    public static string GetDataFromAppDomain(string propertyName)
    {
        var appDomainType = typeof(object).GetTypeInfo().Assembly?.GetType("System.AppDomain");
        var currentDomain = appDomainType?.GetProperty("CurrentDomain")?.GetValue(null);
        var deps = appDomainType?.GetMethod("GetData")?.Invoke(currentDomain, new[] { propertyName });
        return deps as string;
    }
}