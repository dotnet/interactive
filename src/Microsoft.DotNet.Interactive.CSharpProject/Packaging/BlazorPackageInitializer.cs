// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Clockwise;
using Microsoft.DotNet.Interactive.Utility;
using Microsoft.DotNet.Interactive.CSharpProject.Tools;
using Microsoft.DotNet.Interactive.CSharpProject.Utility;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging
{
    public class BlazorPackageInitializer : PackageInitializer
    {
        private readonly string _name;
        private readonly List<(string packageName, string packageVersion, string restoreSources)> _addPackages;

        public BlazorPackageInitializer(string name, List<(string packageName, string packageVersion, string restoreSources)> addPackages) :
            base("blazorwasm", "MLS.Blazor")
        {
            _name = name;
            var packages = addPackages ?? throw new ArgumentNullException(nameof(addPackages));

            var requiredPackages = new List<(string packageName, string packageVersion, string restoreSources)>
            {
                ("Newtonsoft.Json", "13.0.1", ""),
                ("System.CommandLine", "2.0.0-beta1.20359.2", "")
            };

            _addPackages = packages.Concat(requiredPackages).Distinct().ToList();
        }

        public override async Task Initialize(DirectoryInfo directory, Budget budget = null)
        {
            if (directory.Name != "MLS.Blazor")
            {
                throw new ArgumentException(@"Directory must be called `MLS.Blazor` but is actually called {nameof(directory)}");
            }

            await base.Initialize(directory, budget);
            await MakeBlazorProject(directory, budget);
        }

        private async Task MakeBlazorProject(DirectoryInfo directory, Budget budget)
        {
            var dotnet = new Dotnet(directory);
            var root = directory.FullName;

            AddRootNamespaceAndBlazorLinkerDirective();
            DeleteUnusedFilesFromTemplate(root);
            AddEmbeddedResourceContentToProject(root);
            UpdateFileText(root, Path.Combine("wwwroot","index.html"), "/LocalCodeRunner/blazor-console", $"/LocalCodeRunner/{_name}");
            
            foreach (var packageId in _addPackages)
            {
                var addPackageResult = await dotnet.AddPackage(packageId.packageName, packageId.packageVersion);
                var addPackageResultMessage = string.Concat(
                    string.Join("\n", addPackageResult.Output), 
                    string.Join("\n", addPackageResult.Error));

                addPackageResult.ThrowOnFailure(addPackageResultMessage);
            }
           

            var result = await dotnet.Build("-o runtime /bl");
            var stuff = string.Concat(string.Join("\n", result.Output), (string.Join("\n", result.Error)));
            result.ThrowOnFailure(stuff);

            void AddRootNamespaceAndBlazorLinkerDirective()
            {
                UpdateFileText(root, "MLS.Blazor.csproj", "</PropertyGroup>",
                    @"</PropertyGroup>
<PropertyGroup>
    <RootNamespace>MLS.Blazor</RootNamespace>
</PropertyGroup>
<ItemGroup>
  <BlazorLinkerDescriptor Include=""Linker.xml"" />
</ItemGroup>");
            }
        }

        private void AddEmbeddedResourceContentToProject(string root)
        {
            var wwwRootFiles = new[] { "index.html", "interop.js" };
            var pagesFiles = new[] { "Index.razor" };
            var rootFiles = new[] {"App.razor", "Program.cs", "Startup.cs", "CodeRunner.cs", "InteropMessage.cs", "SerializableDiagnostic.cs", "WasmCodeRunnerRequest.cs", "WasmCodeRunnerResponse.cs", "CommandLineBuilderExtensions.cs", "EntryPointDiscoverer.cs", "PreserveConsoleState.cs", "_Imports.razor", "Linker.xml" };

            WriteResourcesToLocation(wwwRootFiles, Path.Combine(root, "wwwroot"));
            WriteResourcesToLocation(pagesFiles, Path.Combine(root, "Pages"));
            WriteResourcesToLocation(rootFiles, root);
        }

        private static void DeleteUnusedFilesFromTemplate(string root)
        {
            var filesAndDirectoriestoDelete = new[] { "Pages", "Shared", "wwwroot", "_Imports.razor" };
            foreach (var fOrD in filesAndDirectoriestoDelete)
            {
                Path.Combine(root, fOrD).DeleteFileSystemObject();
            }
        }

        private void UpdateFileText(string root, string file, string toReplace, string replacement)
        {
            file = Path.Combine(root, file);
            var text = File.ReadAllText(file);
            var updated = text.Replace(toReplace, replacement);
            File.WriteAllText(file, updated);
        }

        private void WriteResourcesToLocation(string[] resources, string targetDirectory)
        {
            foreach (var resource in resources)
            {
                WriteResource(resource, targetDirectory);
            }
        }

        private void WriteResource(string resourceName, string targetDirectory)
        {
            var text = GetType().ReadManifestResource(resourceName);
            Directory.CreateDirectory(targetDirectory);
            var path = Path.Combine(targetDirectory, resourceName);
            File.WriteAllText(path, text);
        }
    }
}
