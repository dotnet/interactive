// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Build;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.CSharpProject.RoslynWorkspaceUtilities;

internal static class BuildCacheFileUtilities
{
    internal static string CacheFilenameSuffix = ".interactive.workspaceData.cache";
    internal static string DirectoryBuildTargetFilename = "Directory.Build.Targets";

    internal static string DirectoryBuildTargetsContent =
        """
<Project>
  <Target Name="CollectProjectData" AfterTargets="Build">
    <ItemGroup>
      <ProjectData Include="ProjectGuid=$(ProjectGuid)">
        <Type>String</Type>
      </ProjectData>
      <ProjectData Include="%(ProjectReference.Identity)">
        <Prefix>ProjectReferences=</Prefix>
        <Type>Array</Type>
      </ProjectData>
      <ProjectData Include="ProjectFilePath=$(MSBuildProjectFullPath)">
        <Type>String</Type>
      </ProjectData>
      <ProjectData Include="LanguageName=C#">
        <Type>String</Type>
      </ProjectData>
      <ProjectData Include="PropertyTargetPath=$(TargetPath)">
        <Type>String</Type>
      </ProjectData>
      <ProjectData Include="%(Compile.FullPath)" Condition="!$([System.String]::new('%(Compile.Identity)').Contains('obj\'))">
        <Prefix>SourceFiles=</Prefix>
        <Type>Array</Type>
      </ProjectData>
      <ProjectData Include="%(ReferencePath.Identity)">
        <Prefix>References=</Prefix>
        <Type>Array</Type>
      </ProjectData>
      <ProjectData Include="%(Analyzer.Identity)">
        <Prefix>AnalyzerReferences=</Prefix>
        <Type>Array</Type>
      </ProjectData>
      <ProjectData Include="$(DefineConstants)">
        <Prefix>PreprocessorSymbols=</Prefix>
        <Type>String</Type>
      </ProjectData>
      <ProjectData Include="PropertyLangVersion=$(LangVersion)">
        <Type>String</Type>
      </ProjectData>
      <ProjectData Include="PropertyOutputType=$(OutputType)">
        <Type>String</Type>
      </ProjectData>
    </ItemGroup>

    <!-- Split PreprocessorSymbols into individual items -->
    <ItemGroup>
      <PreprocessorSymbolItems Include="$(DefineConstants.Split(';'))" />
    </ItemGroup>

    <!-- Transform the ProjectData and PreprocessorSymbolItems to include the prefix -->
    <ItemGroup>
      <ProjectDataLines Include="$([System.String]::Format('{0}{1}', %(ProjectData.Prefix), %(ProjectData.Identity)))" />
      <ProjectDataLines Include="$([System.String]::Format('PreprocessorSymbols={0}', %(PreprocessorSymbolItems.Identity)))" />
    </ItemGroup>

    <!-- Write collected project data to a file -->
    <WriteLinesToFile Lines="@(ProjectDataLines)"
                      File="$(MSBuildProjectFullPath).interactive.workspaceData.cache"
                      Overwrite="True"
                      WriteOnlyWhenDifferent="True" />
  </Target>
</Project>
""";

    internal static async Task BuildAndCreateCacheFileAsync(string csprojFilePath)
    {
        if (string.IsNullOrEmpty(csprojFilePath))
        {
            throw new ArgumentException($"The csproj file path is null or empty");
        }

        if (!File.Exists(csprojFilePath))
        {
            throw new FileNotFoundException($"The csproj file does not exist: {csprojFilePath}");
        }

        DirectoryInfo directoryInfo = new DirectoryInfo(Path.GetDirectoryName(csprojFilePath));
        FileInfo lastBuildErrorLogFile = new FileInfo(Path.Combine(directoryInfo.FullName, ".net-interactive-builderror"));

        CleanObjFolder(directoryInfo);

        string tempDirectoryBuildTarget = Path.Combine(Path.GetDirectoryName(csprojFilePath), DirectoryBuildTargetFilename);

        try
        {
            File.WriteAllText(tempDirectoryBuildTarget, DirectoryBuildTargetsContent);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Failed to create the target file due to unauthorized access: {tempDirectoryBuildTarget}", ex);
        }
        catch (IOException ex)
        {
            throw new IOException($"Failed to create the target file due to an I/O error: {tempDirectoryBuildTarget}", ex);
        }

        var args = $@"""{csprojFilePath}""";

        var result = await new Dotnet(directoryInfo).Build(args: args);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Build failed with exit code {result.ExitCode}. See {lastBuildErrorLogFile.FullName} for details.");
        }
        else if (lastBuildErrorLogFile.Exists)
        {
            lastBuildErrorLogFile.Delete();
        }

        // Clean up the temp project file
        File.Delete(tempDirectoryBuildTarget);

        var cacheFile = Prebuild.FindCacheFile(directoryInfo);

        if (cacheFile is not { Exists: true })
        {
            throw new FileNotFoundException($"Cache file not found after build completion in directory: {directoryInfo.FullName}");
        }
    }

    internal static void CleanObjFolder(DirectoryInfo directoryInfo)
    {
        var targets = directoryInfo.GetDirectories("obj");
        foreach (var target in targets)
        {
            target.Delete(true);
        }
    }
}