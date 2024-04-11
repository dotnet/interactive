// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.CSharpProject.Build;

public class PrebuildBuilder
{
    private Prebuild _prebuild;
    private readonly List<Func<Prebuild, Task>> _afterCreateActions = new();
    private readonly List<(string packageName, string packageVersion, string restoreSources)> _addPackages = new();
    private string _languageVersion = "latest";

    public PrebuildBuilder(string prebuildName, DirectoryInfo directory = null)
    {
        if (string.IsNullOrWhiteSpace(prebuildName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(prebuildName));
        }

        PrebuildName = prebuildName;
        Directory = directory;
    }

    public bool EnableBuild { get; set; }

    public string PrebuildName { get; }

    public IPrebuildInitializer PrebuildInitializer { get; private set; }

    public DirectoryInfo Directory { get; }

    public void UseTemplate(string template, string projectName = null, string language = null)
    {
        PrebuildInitializer = new PrebuildInitializer(
            template,
            projectName ?? PrebuildName,
            language,
            RunAfterCreateActionsAsync);
    }

    public void AddPackageReference(string packageId, string version = null, string restoreSources = null)
    {
        _addPackages.Add((packageId, version, restoreSources));
        _afterCreateActions.Add(async prebuild =>
        {
            Func<Task> action = async () =>
            {
                var dotnet = new Dotnet(prebuild.Directory);
                await dotnet.AddPackage(packageId, version);
            };

            await action();
        });
    }

    public void UseLanguageVersion(string version)
    {
        _languageVersion = version;

        _afterCreateActions.Add(async prebuild =>
        {
            var projectFiles = prebuild.Directory.GetFiles("*.csproj");

            foreach (var projectFile in projectFiles)
            {
                projectFile.SetLanguageVersion(_languageVersion);
            }
        });
    }

    public Prebuild GetPrebuild()
    {
        if (_prebuild is null)
        {
            _prebuild = new Prebuild(
                PrebuildName,
                PrebuildInitializer,
                Directory,
                enableBuild: EnableBuild);
        }

        return _prebuild;
    }

    private async Task RunAfterCreateActionsAsync(DirectoryInfo directoryInfo)
    {
        foreach (var action in _afterCreateActions)
        {
            await action(_prebuild);
        }
    }
}