// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Tests.Utility
{
    public class TestAssemblyReference : IDisposable
    {
        public string ProjectName { get; }
        public string TargetFramework { get; }
        public TemporaryDirectory Directory { get; }

        public TestAssemblyReference(string projectName, string targetFramework, string sourceFileName, string sourceFileContents)
        {
            ProjectName = projectName;
            TargetFramework = targetFramework;
            Directory = new TemporaryDirectory(
                ($"{ProjectName}.csproj", $@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{TargetFramework}</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
</Project>"),
                (sourceFileName, sourceFileContents)
            );
        }

        public async Task<string> BuildAndGetPathToAssembly()
        {
            var dotnet = new Dotnet(Directory.Directory);
            var result = await dotnet.Build();
            result.ThrowOnFailure("Failed to build sample assembly");
            var assemblyPath = Path.Combine(Directory.Directory.FullName, "bin", "Debug", TargetFramework, $"{ProjectName}.dll");
            if (!File.Exists(assemblyPath))
            {
                throw new Exception($"The expected assembly was not found at path '{assemblyPath}'.");
            }

            return assemblyPath;
        }

        public void Dispose()
        {
            Directory.Dispose();
        }
    }
}
