// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging;

public class PackageBuilder
{
    private Package _packageBase;
    private readonly List<Func<Package, Task>> _afterCreateActions = new();
    private readonly List<(string packageName, string packageVersion, string restoreSources)> _addPackages = new();
    private string _languageVersion = "8.0";

    public PackageBuilder(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(packageName));
        }

        PackageName = packageName;
    }

    public string PackageName { get; }

    public IPackageInitializer PackageInitializer { get; private set; }

    public DirectoryInfo Directory { get; set; }
    
    public void CreateUsingDotnet(string template, string projectName = null, string language = null)
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
    
    public void TrySetLanguageVersion(string version)
    {
        _languageVersion = version;

        _afterCreateActions.Add(async package =>
        {
            async Task Action()
            {
                await Task.Yield();
                var projectFiles = package.Directory.GetFiles("*.csproj");

                foreach (var projectFile in projectFiles)
                {
                    projectFile.SetLanguageVersion(_languageVersion);
                }
            }

            await Action();
        });
    }

    public Package GetPackage()
    {
        if (_packageBase is null)
        {
            _packageBase = new Package(
                PackageName,
                PackageInitializer,
                Directory);
        }

        return _packageBase;
    }

    private async Task RunAfterCreateActionsAsync(DirectoryInfo directoryInfo)
    {
        foreach (var action in _afterCreateActions)
        {
            await action(_packageBase);
        }
    }
}