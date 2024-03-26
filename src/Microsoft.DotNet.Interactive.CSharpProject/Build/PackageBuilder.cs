// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.CSharpProject.Build;

public class PackageBuilder
{
    private Package _package;
    private readonly List<Func<Package, Task>> _afterCreateActions = new();
    private readonly List<(string packageName, string packageVersion, string restoreSources)> _addPackages = new();
    private string _languageVersion = "latest";

    public PackageBuilder(string packageName, DirectoryInfo directory = null)
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(packageName));
        }

        PackageName = packageName;
        Directory = directory;
    }

    public bool EnableBuild { get; set; }

    public string PackageName { get; }

    public IPackageInitializer PackageInitializer { get; private set; }

    public DirectoryInfo Directory { get; }

    public void UseTemplate(string template, string projectName = null, string language = null)
    {
        PackageInitializer = new PackageInitializer(
            template,
            projectName ?? PackageName,
            language,
            RunAfterCreateActionsAsync);
    }

    public void AddPackageReference(string packageId, string version = null, string restoreSources = null)
    {
        _addPackages.Add((packageId, version, restoreSources));
        _afterCreateActions.Add(async package =>
        {
            Func<Task> action = async () =>
            {
                var dotnet = new Dotnet(package.Directory);
                await dotnet.AddPackage(packageId, version);
            };

            await action();
        });
    }

    public void UseLanguageVersion(string version)
    {
        _languageVersion = version;

        _afterCreateActions.Add(async package =>
        {
            var projectFiles = package.Directory.GetFiles("*.csproj");

            foreach (var projectFile in projectFiles)
            {
                projectFile.SetLanguageVersion(_languageVersion);
            }
        });
    }

    public Package GetPackage()
    {
        if (_package is null)
        {
            _package = new Package(
                PackageName,
                PackageInitializer,
                Directory,
                enableBuild: EnableBuild);
        }

        return _package;
    }

    private async Task RunAfterCreateActionsAsync(DirectoryInfo directoryInfo)
    {
        foreach (var action in _afterCreateActions)
        {
            await action(_package);
        }
    }
}