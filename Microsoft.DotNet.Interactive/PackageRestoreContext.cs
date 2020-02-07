// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using static Pocket.Logger;

namespace Microsoft.DotNet.Interactive
{
    public class PackageRestoreContext : IDisposable
    {
        private readonly ConcurrentDictionary<string, PackageReference> _requestedPackageReferences = new ConcurrentDictionary<string, PackageReference>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ResolvedPackageReference> _resolvedPackageReferences = new Dictionary<string, ResolvedPackageReference>(StringComparer.OrdinalIgnoreCase);
        private readonly Lazy<DirectoryInfo> _lazyDirectory;

        public PackageRestoreContext()
        {
            _lazyDirectory = new Lazy<DirectoryInfo>(() =>
            {
                var dir = new DirectoryInfo(
                    Path.Combine(
                        Paths.UserProfile,
                        ".net-interactive-csharp",
                        Guid.NewGuid().ToString("N")));

                if (!dir.Exists)
                {
                    dir.Create();
                }

                return dir;
            });

            AssemblyLoadContext.Default.Resolving += OnResolving;
        }

        private Assembly OnResolving(AssemblyLoadContext loadContext, AssemblyName assemblyName)
        {
            var data = _resolvedPackageReferences.Values
                .SelectMany(r => r.AssemblyPaths)
                .Select(p => ( assemblyName: AssemblyName.GetAssemblyName(p.FullName), fileInfo:p )).ToList();
            var found = data
                .FirstOrDefault(a => a.assemblyName.FullName == assemblyName.FullName);

            return found == default ? null : loadContext.LoadFromAssemblyPath(found.fileInfo.FullName);
        }

        public DirectoryInfo Directory => _lazyDirectory.Value;

        public bool AddPackageReference(
            string packageName,
            string packageVersion = null,
            string restoreSources = null)
        {
            var key = $"{packageName}:{restoreSources}";
       
            // we use a lock because we are going to be looking up and inserting
            if (!_requestedPackageReferences.TryGetValue(key, out PackageReference existingPackage))
            {
                if (!_resolvedPackageReferences.TryGetValue(key, out ResolvedPackageReference resolvedPackage))
                {
                    return _requestedPackageReferences.TryAdd(key, new PackageReference(packageName, packageVersion, restoreSources));
                }

                return resolvedPackage.PackageVersion.Trim() == packageVersion.Trim();
            }

            // Verify version numbers match note: wildcards/previews are considered distinct
            return existingPackage.PackageVersion.Trim() == packageVersion.Trim();
        }

        public IEnumerable<PackageReference> RequestedPackageReferences => _requestedPackageReferences.Values;

        public IEnumerable<ResolvedPackageReference> ResolvedPackageReferences => _resolvedPackageReferences.Values;

        public ResolvedPackageReference GetResolvedPackageReference(string packageName) => _resolvedPackageReferences[packageName];

        public async Task<PackageRestoreResult> Restore()
        {
            WriteProjectFile();

            var dotnet = new Dotnet(Directory);
            
            var commandLine = "msbuild -restore /t:WriteNugetAssemblyPaths";

#if DEBUG
            commandLine += " /bl";
#endif

            var result = await dotnet.Execute(commandLine);

            if (result.ExitCode != 0)
            {
                return new PackageRestoreResult(
                    succeeded: false,
                    requestedPackages: _requestedPackageReferences.Values,
                    errors: result.Output.Concat(result.Error).ToArray());
            }
            else
            {
                ReadResolvedReferencesFromBuildOutput();

                return new PackageRestoreResult(
                    succeeded: true,
                    requestedPackages: _requestedPackageReferences.Values,
                    resolvedReferences: _resolvedPackageReferences.Values.ToList());
            }
        }

        private void ReadResolvedReferencesFromBuildOutput()
        {
            var resolvedreferenceFilename = "*.resolvedReferences.paths";
            var nugetPathsFile = Directory.GetFiles(resolvedreferenceFilename).SingleOrDefault();

            if (nugetPathsFile == null)
            {
                Log.Error($"File not found: {Directory.FullName}{Path.DirectorySeparatorChar}{resolvedreferenceFilename}");
                return;
            }

            var nugetPackageLines = File.ReadAllText(Path.Combine(Directory.FullName, nugetPathsFile.FullName))
                                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var probingPaths = new List<DirectoryInfo>();

            var resolved = nugetPackageLines
                       .Select(line => line.Split(','))
                       .Where(line =>
                       {
                           if (string.IsNullOrWhiteSpace(line[0]))
                           {
                               if (!string.IsNullOrWhiteSpace(line[3]))
                               {
                                   probingPaths.Add(new DirectoryInfo(line[3]));
                               }

                               return false;
                           }

                           return true;
                       })
                       .Select(line =>
                                   (
                                       packageName: line[0].Trim(),
                                       packageVersion: line[1].Trim(),
                                       assemblyPath: new FileInfo(line[2].Trim()),
                                       packageRoot: !string.IsNullOrWhiteSpace(line[3])
                                                        ? new DirectoryInfo(line[3].Trim())
                                                        : null,
                                       runtimeIdentifier: line[4].Trim()))
                       .GroupBy(x =>
                                    (
                                        x.packageName,
                                        x.packageVersion,
                                        x.packageRoot))
                       .Select(xs => new ResolvedPackageReference(
                                   xs.Key.packageName,
                                   xs.Key.packageVersion,
                                   xs.Select(x => x.assemblyPath).ToArray(),
                                   xs.Key.packageRoot,
                                   probingPaths))
                       .ToArray();

            foreach (var reference in resolved)
            {
                _resolvedPackageReferences.TryAdd(reference.PackageName, reference);
            }
        }

        private void WriteProjectFile()
        {
            var directoryPropsContent =
                $@"
<Project Sdk='Microsoft.NET.Sdk'>
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>netcoreapp3.1</TargetFramework>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    {PackageReferences()}
    {Targets()}
    
</Project>";

            File.WriteAllText(
                Path.Combine(
                    Directory.FullName,
                    "r.csproj"),
                directoryPropsContent);

            File.WriteAllText(
                Path.Combine(
                    Directory.FullName,
                    "Program.cs"),
                @"
using System;

namespace s
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}
");

            string PackageReferences()
            {
                string GetReferenceVersion(PackageReference reference)
                {
                    return string.IsNullOrEmpty(reference.PackageVersion) ? "*" : reference.PackageVersion;
                }

                var sb = new StringBuilder();

                sb.Append("  <ItemGroup>\n");

                _requestedPackageReferences
                    .Values
                    .Where(reference => !string.IsNullOrEmpty(reference.PackageName))
                    .ToList()
                    .ForEach(reference => sb.Append($"    <PackageReference Include=\"{reference.PackageName}\" Version=\"{GetReferenceVersion(reference)}\"/>\n"));

                sb.Append("  </ItemGroup>\n");

                sb.Append("  <PropertyGroup>\n");

                _requestedPackageReferences
                    .Values
                    .Where(reference => !string.IsNullOrEmpty(reference.RestoreSources))
                    .ToList()
                    .ForEach(reference => sb.Append($"    <RestoreAdditionalProjectSources>$(RestoreAdditionalProjectSources){reference.RestoreSources}</RestoreAdditionalProjectSources>\n"));
                sb.Append("  </PropertyGroup>\n");

                return sb.ToString();
            }

            string Targets() => @"
  <Target Name=""ComputePackageRootsForInteractivePackageManagement""
          DependsOnTargets=""ResolveReferences;ResolveSdkReferences;ResolveTargetingPackAssets;ResolveSDKReferences;GenerateBuildDependencyFile"">

      <ItemGroup>
        <__InteractiveReferencedAssemblies Include = ""@(ReferencePath)"" />
        <__InteractiveReferencedAssembliesCopyLocal Include = ""@(RuntimeCopyLocalItems)"" Condition=""'$(TargetFrameworkIdentifier)'!='.NETFramework'"" />
        <__InteractiveReferencedAssembliesCopyLocal Include = ""@(ReferenceCopyLocalPaths)"" Condition=""'$(TargetFrameworkIdentifier)'=='.NETFramework'"" />
        <__ConflictsList Include=""%(_ConflictPackageFiles.ConflictItemType)=%(_ConflictPackageFiles.Filename)%(_ConflictPackageFiles.Extension)"" />
      </ItemGroup>
      <PropertyGroup>
        <__Conflicts>@(__ConflictsList, ';')</__Conflicts>
      </PropertyGroup>

      <ItemGroup>
        <InteractiveResolvedFile Include=""@(__InteractiveReferencedAssemblies)""
                                 Condition=""$([System.String]::new($(__Conflicts)).Contains($([System.String]::new('Reference=%(__InteractiveReferencedAssemblies.Filename)%(__InteractiveReferencedAssemblies.Extension)'))))""
                                 KeepDuplicates=""false"">
            <NormalizedIdentity Condition=""'%(Identity)'!=''"">$([System.String]::Copy('%(Identity)').Replace('\', '/'))</NormalizedIdentity>
            <NormalizedPathInPackage Condition=""'%(__InteractiveReferencedAssemblies.PathInPackage)'!=''"">$([System.String]::Copy('%(__InteractiveReferencedAssemblies.PathInPackage)').Replace('\', '/'))</NormalizedPathInPackage>
            <PositionPathInPackage Condition=""'%(InteractiveResolvedFile.NormalizedPathInPackage)'!=''"">$([System.String]::Copy('%(InteractiveResolvedFile.NormalizedIdentity)').IndexOf('%(InteractiveResolvedFile.NormalizedPathInPackage)'))</PositionPathInPackage>
            <PackageRoot Condition=""'%(InteractiveResolvedFile.NormalizedPathInPackage)'!='' and '%(InteractiveResolvedFile.PositionPathInPackage)'!='-1'"">$([System.String]::Copy('%(InteractiveResolvedFile.NormalizedIdentity)').Substring(0, %(InteractiveResolvedFile.PositionPathInPackage)))</PackageRoot>
            <InitializeSourcePath>%(InteractiveResolvedFile.PackageRoot)content\%(__InteractiveReferencedAssemblies.FileName)%(__InteractiveReferencedAssemblies.Extension).fsx</InitializeSourcePath>
            <IsNotImplementationReference>$([System.String]::Copy('%(__InteractiveReferencedAssemblies.PathInPackage)').StartsWith('ref/'))</IsNotImplementationReference>
            <NuGetPackageId>%(__InteractiveReferencedAssemblies.NuGetPackageId)</NuGetPackageId>
            <NuGetPackageVersion>%(__InteractiveReferencedAssemblies.NuGetPackageVersion)</NuGetPackageVersion>
        </InteractiveResolvedFile>

        <InteractiveResolvedFile Include=""@(__InteractiveReferencedAssembliesCopyLocal)"" KeepDuplicates=""false"">
            <NormalizedIdentity Condition=""'%(Identity)'!=''"">$([System.String]::Copy('%(Identity)').Replace('\', '/'))</NormalizedIdentity>
            <NormalizedPathInPackage Condition=""'%(__InteractiveReferencedAssembliesCopyLocal.PathInPackage)'!=''"">$([System.String]::Copy('%(__InteractiveReferencedAssembliesCopyLocal.PathInPackage)').Replace('\', '/'))</NormalizedPathInPackage>
            <PositionPathInPackage Condition=""'%(InteractiveResolvedFile.NormalizedPathInPackage)'!=''"">$([System.String]::Copy('%(InteractiveResolvedFile.NormalizedIdentity)').IndexOf('%(InteractiveResolvedFile.NormalizedPathInPackage)'))</PositionPathInPackage>
            <PackageRoot Condition=""'%(InteractiveResolvedFile.NormalizedPathInPackage)'!='' and '%(InteractiveResolvedFile.PositionPathInPackage)'!='-1'"">$([System.String]::Copy('%(InteractiveResolvedFile.NormalizedIdentity)').Substring(0, %(InteractiveResolvedFile.PositionPathInPackage)))</PackageRoot>
            <InitializeSourcePath>%(InteractiveResolvedFile.PackageRoot)content\%(__InteractiveReferencedAssembliesCopyLocal.FileName)%(__InteractiveReferencedAssembliesCopyLocal.Extension).fsx</InitializeSourcePath>
            <IsNotImplementationReference>$([System.String]::Copy('%(__InteractiveReferencedAssembliesCopyLocal.PathInPackage)').StartsWith('ref/'))</IsNotImplementationReference>
            <NuGetPackageId>%(__InteractiveReferencedAssembliesCopyLocal.NuGetPackageId)</NuGetPackageId>
            <NuGetPackageVersion>%(__InteractiveReferencedAssembliesCopyLocal.NuGetPackageVersion)</NuGetPackageVersion>
        </InteractiveResolvedFile>

        <NativeIncludeRoots
            Include=""@(RuntimeTargetsCopyLocalItems)""
            Condition=""'%(RuntimeTargetsCopyLocalItems.AssetType)' == 'native'"">
            <Path>$([MSBuild]::EnsureTrailingSlash('$([System.String]::Copy('%(FullPath)').Substring(0, $([System.String]::Copy('%(FullPath)').LastIndexOf('runtimes'))))'))</Path>
        </NativeIncludeRoots>
      </ItemGroup>
  </Target>

  <Target Name='WriteNugetAssemblyPaths' 
          DependsOnTargets=""ComputePackageRootsForInteractivePackageManagement""
          BeforeTargets=""CoreCompile""
          AfterTargets=""PrepareForBuild"">

    <ItemGroup>
      <ResolvedReferenceLines Remove='*' />
      <ResolvedReferenceLines Include='%(InteractiveResolvedFile.NugetPackageId),%(InteractiveResolvedFile.NugetPackageVersion),%(InteractiveResolvedFile.Identity),%(NativeIncludeRoots.Path),$(AppHostRuntimeIdentifier)' />
    </ItemGroup>

    <WriteLinesToFile Lines='@(ResolvedReferenceLines)' 
                      File='$(MSBuildProjectFullPath).resolvedReferences.paths' 
                      Overwrite='True' WriteOnlyWhenDifferent='True' />
  </Target>
";
        }

        public void Dispose()
        {
            try
            {
                AssemblyLoadContext.Default.Resolving -= OnResolving;
                if (_lazyDirectory.IsValueCreated)
                {
                    Directory.Delete(true);
                }
            }
            catch
            {
            }
        }

     
    }
}