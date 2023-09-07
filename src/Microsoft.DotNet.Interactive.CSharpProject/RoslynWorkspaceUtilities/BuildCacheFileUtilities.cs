using Microsoft.DotNet.Interactive.CSharpProject.Packaging;
using Microsoft.DotNet.Interactive.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.CSharpProject.RoslynWorkspaceUtilities;

internal static class BuildCacheFileUtilities
{
    internal static string cacheFilenameSuffix = ".interactive.workspaceData.cache";
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

    internal static async Task<string> BuildAndCreateCacheFileAsync(string csprojFilePath)
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(Path.GetDirectoryName(csprojFilePath));
        FileInfo lastBuildErrorLogFile = new FileInfo(Path.Combine(directoryInfo.FullName, ".net-interactive-builderror"));

        CleanObjFolder(directoryInfo);

        string tempDirectoryBuildTarget =
            Path.Combine(Path.GetDirectoryName(csprojFilePath), DirectoryBuildTargetFilename);
        File.WriteAllText(tempDirectoryBuildTarget, DirectoryBuildTargetsContent);

        var args = "";
        if (Path.Exists(csprojFilePath))
        {
            args = $@"""{csprojFilePath}"" {args}";
        }

        var result = await new Dotnet(directoryInfo).Build(args: args);

        if (result.ExitCode != 0)
        {
            File.WriteAllText(
                lastBuildErrorLogFile.FullName,
                string.Join(Environment.NewLine, result.Error));
        }
        else if (lastBuildErrorLogFile.Exists)
        {
            lastBuildErrorLogFile.Delete();
        }

        // Clean up the temp project file
        File.Delete(tempDirectoryBuildTarget);

        result.ThrowOnFailure();

        return string.Empty;
    }

    private static void CleanObjFolder(DirectoryInfo directoryInfo)
    {
        var targets = directoryInfo.GetDirectories("obj");
        foreach (var target in targets)
        {
            target.Delete(true);
        }
    }

    public static FileInfo FindCacheFile(DirectoryInfo directoryInfo) => directoryInfo.GetFiles("*" + cacheFilenameSuffix).FirstOrDefault();
}
