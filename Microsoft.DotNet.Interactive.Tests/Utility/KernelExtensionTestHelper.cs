// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Tests.Utility
{
    internal static class KernelExtensionTestHelper
    {
        private static readonly string _microsoftDotNetInteractiveDllPath = typeof(IKernelExtension).Assembly.Location;

        internal static async Task<FileInfo> CreateExtensionNupkg(
            DirectoryInfo projectDir,
            string code,
            string packageName,
            string packageVersion)
        {
            projectDir.Populate(
                ExtensionCs(code),
                ("Extension.csproj", $@"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <IsPackable>true</IsPackable>
    <PackageId>{packageName}</PackageId>
    <PackageVersion>{packageVersion}</PackageVersion>
  </PropertyGroup>

  <ItemGroup>
    <None Include=""$(OutputPath)/Extension.dll"" Pack=""true"" PackagePath=""interactive-extensions/dotnet"" />
  </ItemGroup>

  <ItemGroup>
    <Reference Include=""Microsoft.DotNet.Interactive"">
      <HintPath>{_microsoftDotNetInteractiveDllPath}</HintPath>
    </Reference>
  </ItemGroup>

</Project>

"));

            var dotnet = new Dotnet(projectDir);

            var pack = await dotnet.Pack(projectDir.FullName);

            pack.ThrowOnFailure();

            return projectDir
                   .GetFiles("*.nupkg", SearchOption.AllDirectories)
                   .Single();
        }

        internal static async Task<FileInfo> CreateExtensionAssembly(
            DirectoryInfo projectDir,
            string code,
            DirectoryInfo copyDllTo = null,
            [CallerMemberName] string testName = null)
        {
            var extensionName = AlignExtensionNameWithDirectoryName(projectDir, testName);

            await CreateExtensionProjectAndBuild(
                projectDir,
                code,
                extensionName);

            var extensionDll = projectDir
                               .GetDirectories("bin", SearchOption.AllDirectories)
                               .Single()
                               .GetFiles($"{extensionName}.dll", SearchOption.AllDirectories)
                               .Single();

            if (copyDllTo != null)
            {
                if (!copyDllTo.Exists)
                {
                    copyDllTo.Create();
                }

                var finalExtensionDll = new FileInfo(Path.Combine(copyDllTo.FullName, extensionDll.Name));
                File.Move(extensionDll.FullName, finalExtensionDll.FullName);
                extensionDll = finalExtensionDll;
            }

            return extensionDll;
        }

        private static string AlignExtensionNameWithDirectoryName(DirectoryInfo extensionDir, string testName)
        {
            var match = Regex.Match(extensionDir.Name, @"(?<counter>\.\d+)$");
            return match.Success ? $"{testName}{match.Groups["counter"].Value}" : testName;
        }

        private static async Task CreateExtensionProjectAndBuild(
            DirectoryInfo projectDir,
            string code,
            string extensionName)
        {
            projectDir.Populate(
                ExtensionCs(code),
                ("TestExtension.csproj", $@"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <AssemblyName>{extensionName}</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include=""Microsoft.DotNet.Interactive"">
      <HintPath>{_microsoftDotNetInteractiveDllPath}</HintPath>
    </Reference>
  </ItemGroup>

</Project>
"));

            var buildResult = await new Dotnet(projectDir).Build();

            buildResult.ThrowOnFailure();
        }

        private static (string, string) ExtensionCs(string code)
        {
            return ("Extension.cs", $@"
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;

public class TestKernelExtension : IKernelExtension
{{
    public async Task OnLoadAsync(IKernel kernel)
    {{
        {code}
    }}
}}
");
        }
    }
}