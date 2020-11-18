// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Tests.Utility
{
    public static class KernelExtensionTestHelper
    {
        private static readonly string _microsoftDotNetInteractiveDllPath = typeof(IKernelExtension).Assembly.Location;

        public static async Task<FileInfo> CreateExtensionNupkg(
            DirectoryInfo projectDir,
            string code,
            string packageName,
            string packageVersion,
            params FileInfo[] filesToEmbed)
        {
            var msbuildFragment = GenerateEmbeddedResourceFragment(filesToEmbed);


            var extensionCode = filesToEmbed?.Length == 0 ? ExtensionCs(code) : FileProviderExtensionCs(code);
            projectDir.Populate(
                extensionCode,
                ("Extension.csproj", $@"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net5.0</TargetFramework>
    <IsPackable>true</IsPackable>
    <PackageId>{packageName}</PackageId>
    <PackageVersion>{packageVersion}</PackageVersion>
    <AssemblyName>{packageName}</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <None Include=""$(OutputPath)/{packageName}.dll"" Pack=""true"" PackagePath=""interactive-extensions/dotnet"" />
  </ItemGroup>

{msbuildFragment}

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

        private static string GenerateEmbeddedResourceFragment(FileInfo[] filesToEmbed)
        {
            if (filesToEmbed?.Length == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.AppendLine(@"   <ItemGroup>");
            foreach (var fileInfo in filesToEmbed)
            {
                builder.AppendLine($@"      <FilesToEmbed Include=""{fileInfo.FullName}"" />");
            }
            builder.AppendLine(@"      <EmbeddedResource Include=""@(FilesToEmbed)"" LogicalName=""$(AssemblyName).resources.%(FileName)%(Extension)""  />");
            builder.AppendLine(@"   </ItemGroup>");

            return builder.ToString();
        }

        public static async Task<FileInfo> CreateExtensionAssembly(
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
    <TargetFramework>net5.0</TargetFramework>
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
    public async Task OnLoadAsync(Kernel kernel)
    {{
        {code}
    }}
}}
");

        }

        private static (string, string) FileProviderExtensionCs(string code)
        {
            return ("Extension.cs", $@"
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;

public class TestKernelExtension : IKernelExtension, IStaticContentSource
{{
    public async Task OnLoadAsync(Kernel kernel)
    {{
        {code}
    }}

    public string  Name => ""TestKernelExtension"";
}}
");

        }
    }
}