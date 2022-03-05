// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging
{
    public class PackageBuilder
    {
        private PackageBase _packageBase;
        private readonly List<Func<PackageBase, Budget, Task>> _afterCreateActions = new List<Func<PackageBase, Budget, Task>>();
       private readonly List<(string packageName, string packageVersion, string restoreSources)> _addPackages = new List<(string packageName, string packageVersion, string restoreSources)>();
        private string _languageVersion = "8.0";

        public PackageBuilder(string packageName, IPackageInitializer packageInitializer = null)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(packageName));
            }

            PackageName = packageName;
            PackageInitializer = packageInitializer;
        }

        public string PackageName { get; }

        public IPackageInitializer PackageInitializer { get; private set; }

        public DirectoryInfo Directory { get; set; }

        public bool CreateRebuildablePackage { get; set; }

        public bool BlazorSupported { get; private set; }

        public void CreateUsingDotnet(string template, string projectName = null, string language = null)
        {
            PackageInitializer = new PackageInitializer(
               template,
               projectName ?? PackageName,
               language,
               AfterCreate);
        }

        public void AddPackageReference(string packageId, string version = null, string restoreSources = null)
        {
            _addPackages.Add((packageId, version, restoreSources));
            _afterCreateActions.Add(async (package, budget) =>
            {
                Func<Task> action = async () =>
                {
                    var dotnet = new Dotnet(package.Directory);
                    await dotnet.AddPackage(packageId, version);
                };

                await action();

            });
        }

        public void SetLanguageVersion(string version)
        {
            _languageVersion = version;

            _afterCreateActions.Add(async (package, budget) =>
            {
                async Task Action()
                {
                    await Task.Yield();
                    var projects = package.Directory.GetFiles("*.csproj");

                    foreach (var project in projects)
                    {
                        project.SetLanguageVersion(_languageVersion);
                    }
                }

                await Action();
            });
        }

        public void TrySetLanguageVersion(string version)
        {
            _languageVersion = version;

            _afterCreateActions.Add(async (package, budget) =>
            {
                async Task Action()
                {
                    await Task.Yield();
                    var projects = package.Directory.GetFiles("*.csproj");

                    foreach (var project in projects)
                    {
                        project.TrySetLanguageVersion(_languageVersion);
                    }
                }

                await Action();
            });
        }

        public void DeleteFile(string relativePath)
        {
            _afterCreateActions.Add(async (workspace, budget) =>
            {
                await Task.Yield();
                var filePath = Path.Combine(workspace.Directory.FullName, relativePath);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            });
        }

        public void WriteFile(string relativePath, string content)
        {
            _afterCreateActions.Add(async (workspace, budget) =>
            {
                await Task.Yield();
                var filePath = Path.Combine(workspace.Directory.FullName, relativePath);
                File.WriteAllText(filePath,content);
            });
        }

        public PackageBase GetPackage(Budget budget = null)
        {
            budget = budget ?? new Budget();

            if (_packageBase == null)
            {
                if (PackageInitializer is BlazorPackageInitializer)
                {
                    _packageBase = new BlazorPackage(
                            PackageName,
                            PackageInitializer,
                            Directory);
                }
                else if (CreateRebuildablePackage)
                {
                    _packageBase = new RebuildablePackage(
                            PackageName,
                            PackageInitializer,
                            Directory);
                }
                else
                {
                    _packageBase = new NonrebuildablePackage(
                            PackageName,
                            PackageInitializer,
                            Directory);
                }
            }

            budget?.RecordEntry();

            return _packageBase;
        }

        private async Task AfterCreate(DirectoryInfo directoryInfo, Budget budget)
        {
            foreach (var action in _afterCreateActions)
            {
                await action(_packageBase, budget);
            }
        }
    }
}
