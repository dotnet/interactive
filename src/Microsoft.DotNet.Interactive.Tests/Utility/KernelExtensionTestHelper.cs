// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Tests.Utility;

public record ExtensionPackage(string PackageLocation, string Name, string Version);

public static class KernelExtensionTestHelper
{
    private static readonly AsyncLazy<ExtensionPackage> _simpleExtensionPackage = new(async () =>
    {
        var projectDir = DirectoryUtility.CreateDirectory();

        var packageName = $"MyTestExtension.{Path.GetRandomFileName()}";
        var packageVersion = "2.0.0-" + Guid.NewGuid().ToString("N");

        return await CreateExtensionNupkg(
            projectDir,
            "await kernel.SendAsync(new SubmitCode(\"\\\"SimpleExtension\\\"\"));",
            packageName,
            packageVersion,
            timeout: TimeSpan.FromMinutes(5));
    });

    private static readonly AsyncLazy<ExtensionPackage> _fileProviderExtensionPackage = new(async () =>
    {
        var projectDir = DirectoryUtility.CreateDirectory();
        var fileToEmbed = new FileInfo(Path.Combine(projectDir.FullName, "file.txt"));
        await File.WriteAllTextAsync(fileToEmbed.FullName, "for testing only");
        var packageName = $"MyTestExtension.{Path.GetRandomFileName()}";
        var packageVersion = "2.0.0-" + Guid.NewGuid().ToString("N");

        return await CreateExtensionNupkg(
            projectDir,
            "await kernel.SendAsync(new SubmitCode(\"\\\"FileProviderExtension\\\"\"));",
            packageName,
            packageVersion,
            fileToEmbed: fileToEmbed,
            timeout: TimeSpan.FromMinutes(5));
    });

    private static readonly AsyncLazy<ExtensionPackage> _scriptBasedExtensionPackage = new(async () =>
    {
        var projectDir = DirectoryUtility.CreateDirectory();
        var packageName = $"MyTestExtension.{Path.GetRandomFileName()}";
        var packageVersion = "2.0.0-" + Guid.NewGuid().ToString("N");

        var extensionScriptPath = new FileInfo(Path.Combine(projectDir.FullName, "extension.dib"));
        var extensionScriptContent = @"
#!markdown

# This is an extension!

#!csharp
""ScriptExtension""
";
        File.WriteAllText(extensionScriptPath.FullName, extensionScriptContent);

        return await CreateExtensionNupkg(
            projectDir,
            "// this extension does nothing from the assembly",
            packageName,
            packageVersion,
            additionalPackageFiles: new[] { (extensionScriptPath, "interactive-extensions/dotnet") },
            timeout: TimeSpan.FromMinutes(5));
    });

    private static readonly string _microsoftDotNetInteractiveDllPath = typeof(IKernelExtension).Assembly.Location;

    public static Task<ExtensionPackage> GetSimpleExtensionAsync() => _simpleExtensionPackage.ValueAsync();

    public static Task<ExtensionPackage> GetFileProviderExtensionAsync() => _fileProviderExtensionPackage.ValueAsync();

    public static Task<ExtensionPackage> GetScriptExtensionPackageAsync() => _scriptBasedExtensionPackage.ValueAsync();

    public static async Task<ExtensionPackage> CreateExtensionNupkg(
        DirectoryInfo projectDir,
        string code,
        string packageName,
        string packageVersion,
        IReadOnlyCollection<PackageReference> packageReferences = null,
        FileInfo fileToEmbed = null,
        (FileInfo content, string packagePath)[] additionalPackageFiles = null,
        TimeSpan? timeout = null)
    {
        var packageReferencesXml = GeneratePackageReferencesFragment(packageReferences);
        var embeddedResourcesXml = GenerateEmbeddedResourceFragment(fileToEmbed);

        additionalPackageFiles ??= Array.Empty<(FileInfo, string)>();
        var allPackageFiles = new List<(string filePath, string packagePath)>();
        allPackageFiles.Add(($"$(OutputPath)/{packageName}.dll", "interactive-extensions/dotnet"));
        allPackageFiles.AddRange(additionalPackageFiles.Select(item => (item.content.FullName, item.packagePath)));

        var extensionCode = fileToEmbed is null
            ? ExtensionCs(code)
            : FileProviderExtensionCs(code);

        projectDir.Populate(
            extensionCode,
            ("Extension.csproj", $@"
<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>true</IsPackable>
    <PackageId>{packageName}</PackageId>
    <PackageVersion>{packageVersion}</PackageVersion>
    <AssemblyName>{packageName}</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    {string.Join("\n", allPackageFiles.Select(item =>
        new XElement("None",
            new XAttribute("Include", item.filePath),
            new XAttribute("Pack", "true"),
            new XAttribute("PackagePath", item.packagePath)
        ).ToString()))}
  </ItemGroup>

  {packageReferencesXml}

  {embeddedResourcesXml}

  <ItemGroup>
    <Reference Include=""Microsoft.DotNet.Interactive"">
      <HintPath>{_microsoftDotNetInteractiveDllPath}</HintPath>
    </Reference>
  </ItemGroup>

</Project>

"),
            ("global.json", @"{
  ""sdk"": {
    ""version"": ""8.0.100-alpha.1.23061.8"",
    ""allowPrerelease"": true,
    ""rollForward"": ""latestMinor""
  }
}
"));

        var dotnet = new Dotnet(projectDir);

        var pack = await dotnet.Pack(projectDir.FullName, timeout);

        pack.ThrowOnFailure();

        var packageFile = projectDir
            .GetFiles("*.nupkg", SearchOption.AllDirectories)
            .Single();

        return new ExtensionPackage(packageFile.Directory.FullName, packageName, packageVersion);
    }

    private static string GeneratePackageReferencesFragment(IReadOnlyCollection<PackageReference> packageReferences = null)
    {
        if (packageReferences is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        builder.AppendLine(@"   <ItemGroup>");

        foreach (var @ref in packageReferences)
        {
            builder.AppendLine($@"    <PackageReference Include=""{@ref.PackageName}"" Version=""{@ref.PackageVersion}"" />");
        }

        builder.AppendLine(@"   </ItemGroup>");

        return builder.ToString();
    }

    private static string GenerateEmbeddedResourceFragment(FileInfo filesToEmbed)
    {
        if (filesToEmbed is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.AppendLine(@"   <ItemGroup>");
        builder.AppendLine($@"      <FilesToEmbed Include=""{filesToEmbed.FullName}"" />");
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
            .Single(f => f.Directory.Name != "ref");

        if (copyDllTo is not null)
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
    <TargetFramework>net8.0</TargetFramework>
    <AssemblyName>{extensionName}</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include=""Microsoft.DotNet.Interactive"">
      <HintPath>{_microsoftDotNetInteractiveDllPath}</HintPath>
    </Reference>
  </ItemGroup>

</Project>
"),
            ("global.json", @"{
  ""sdk"": {
    ""version"": ""8.0.100-alpha.1.23061.8"",
    ""allowPrerelease"": true,
    ""rollForward"": ""latestMinor""
  }
}
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