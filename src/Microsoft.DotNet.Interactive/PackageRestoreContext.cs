// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using static Pocket.Logger;
using Microsoft.DotNet.DependencyManager;
using System.Globalization;

namespace Microsoft.DotNet.Interactive
{
    public class PackageRestoreContext : IDisposable
    {
        private const string restoreTfm = "net5.0";
        private readonly ConcurrentDictionary<string, PackageReference> _requestedPackageReferences = new ConcurrentDictionary<string, PackageReference>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ResolvedPackageReference> _resolvedPackageReferences = new Dictionary<string, ResolvedPackageReference>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _restoreSources = new HashSet<string>();
        private readonly DependencyProvider _dependencies;

        // Resolution will  after 3 minutes by default
        private int _resolutionTimeout = 180000;

        public PackageRestoreContext()
        {
            // By default look in to the package sources
            //    "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json"
            AddRestoreSource("https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json");
            _dependencies = new DependencyProvider(AssemblyProbingPaths, NativeProbingRoots);
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        private IEnumerable<string> AssemblyProbingPaths()
        {
            foreach (var package in _resolvedPackageReferences.Values)
            {
                foreach (var path in package.AssemblyPaths)
                {
                    yield return path;
                }
            }
        }

        private IEnumerable<string> NativeProbingRoots ()
        {
            foreach (var package in _resolvedPackageReferences.Values)
            {
                foreach (var path in package.ProbingPaths)
                {
                    yield return path;
                }
            }
        }

        public void AddRestoreSource(string source) => _restoreSources.Add(source);

        public PackageReference GetOrAddPackageReference(
            string packageName,
            string packageVersion = null)
        {
            // Package names are case insensitive.
            var key = packageName.ToLower(CultureInfo.InvariantCulture);

            if (_resolvedPackageReferences.TryGetValue(key, out var resolvedPackage))
            {
                if (string.IsNullOrWhiteSpace(packageVersion) ||
                    packageVersion == "*" ||
                    string.Equals(resolvedPackage.PackageVersion.Trim(), packageVersion.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return resolvedPackage;
                }
                else
                {
                    // It was previously resolved at a different version than the one requested
                    return null;
                }
            }

            // we use a lock because we are going to be looking up and inserting
            if (_requestedPackageReferences.TryGetValue(key, out PackageReference existingPackage))
            {
                if (string.Equals(existingPackage.PackageVersion.Trim(), packageVersion.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return existingPackage;
                }
                else
                {
                    return null;
                }
            }

            // Verify version numbers match note: wildcards/previews are considered distinct
            var newPackageRef = new PackageReference(packageName, packageVersion);
            _requestedPackageReferences.TryAdd(key, newPackageRef);
            return newPackageRef;
        }

        public IEnumerable<string> RestoreSources => _restoreSources;

        public IEnumerable<PackageReference> RequestedPackageReferences => _requestedPackageReferences.Values;

        public IEnumerable<ResolvedPackageReference> ResolvedPackageReferences => _resolvedPackageReferences.Values;

        public ResolvedPackageReference GetResolvedPackageReference(string packageName) => _resolvedPackageReferences[packageName];

        private IEnumerable<Tuple<string, string>> GetPackageManagerLines()
        {
            // return restore sources
            foreach( var rs in RestoreSources)
            {
                yield return Tuple.Create("i", rs);
            }
            foreach (var pr in RequestedPackageReferences)
            {
                yield return Tuple.Create("r", $"Include={pr.PackageName}, Version={pr.PackageVersion}");
            }
        }

        private bool TryGetPackageAndVersionFromPackageRoot(DirectoryInfo packageRoot, out PackageReference packageReference)
        {
            // packageRoot looks similar to:
            //    C:/Users/userid/.nuget/packages/fsharp.data/3.3.3/
            //    3.3.3 is the package version
            // fsharp.data is the package name
            var packageName = packageRoot?.Parent?.Name;
            var packageVersion = packageRoot?.Name;

            if (!string.IsNullOrWhiteSpace(packageName) && 
                !string.IsNullOrWhiteSpace(packageVersion))
            {
                try
                {
                    if (_requestedPackageReferences.TryGetValue(packageName.ToLower(CultureInfo.InvariantCulture), out var requested))
                    {
                        packageName = requested.PackageName;
                    }

                    packageReference = new PackageReference(packageName, packageVersion);
                    return true;
                }
                catch (Exception)
                {
                }
            }

            packageReference = default;
            return false;
        }

        private IEnumerable<FileInfo> GetAssemblyPathsForPackage(DirectoryInfo root, IEnumerable<FileInfo> resolutions)
        {
            foreach (var resolution in resolutions)
            {
                // Is the resolution within the package
                if (resolution.DirectoryName.StartsWith(root.FullName))
                {
                    yield return resolution;
                }
            }
        }

        private IEnumerable<ResolvedPackageReference> GetResolvedPackageReferences(
            IEnumerable<FileInfo> resolutions,
            IEnumerable<DirectoryInfo> packageRoots)
        {
            var resolutionsArray = resolutions.ToArray();

            foreach (var root in packageRoots)
            {
                if (TryGetPackageAndVersionFromPackageRoot(root, out var packageReference))
                {
                    var probingPaths = new List<string>
                    {
                        root.FullName
                    };

                    yield return new ResolvedPackageReference(
                        packageReference.PackageName,
                        packageReference.PackageVersion,
                        GetAssemblyPathsForPackage(root, resolutionsArray)
                            .Select(p => p.FullName)
                            .ToArray(),
                        root.FullName,
                        probingPaths);
                }
            }
        }

        private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (args.LoadedAssembly.IsDynamic ||
                string.IsNullOrWhiteSpace(args.LoadedAssembly.Location))
            {
                return;
            }

            Log.Info("OnAssemblyLoad: {location}", args.LoadedAssembly.Location);
        }

        private IResolveDependenciesResult Resolve(IEnumerable<Tuple<string, string>> packageManagerTextLines, string executionTfm, ResolvingErrorReport reportError)
        {
            IDependencyManagerProvider iDependencyManager = _dependencies.TryFindDependencyManagerByKey(Enumerable.Empty<string>(), "", reportError, "nuget");
            if (iDependencyManager == null)
            {
                // If this happens it is because of a bug in the Dependency provider. or deployment failed to deploy the nuget provider dll.
                // We guarantee the presence of the nuget provider, by shipping it with the notebook product
                throw new InvalidOperationException("Internal error - unable to locate the nuget package manager, please try to reinstall.");
            }

            return _dependencies.Resolve(iDependencyManager, ".csx", packageManagerTextLines, reportError, executionTfm, default(string), default(string), default(string), default(string), _resolutionTimeout);        }

        public async Task<PackageRestoreResult> RestoreAsync()
        {
            var newlyRequested = _requestedPackageReferences
                                 .Select(r => r.Value)
                                 .Where(r => !_resolvedPackageReferences.ContainsKey(r.PackageName.ToLower(CultureInfo.InvariantCulture)))
                                 .ToArray();

            var errors = new List<string>();

            var result =
                await Task.Run(() => 
                     Resolve(GetPackageManagerLines(), restoreTfm, ReportError)
                );

            PackageRestoreResult packageRestoreResult;

            if (!result.Success)
            {
                errors.AddRange(result.StdOut);
                packageRestoreResult = new PackageRestoreResult(
                    succeeded: false,
                    requestedPackages: newlyRequested,
                    errors: errors);
            }
            else
            {
                var previouslyResolved = _resolvedPackageReferences.Values.ToArray();

                var resolved = GetResolvedPackageReferences(result.Resolutions.Select(r => new FileInfo(r)),
                                                            result.Roots.Select(r => new DirectoryInfo(r)));

                foreach (var reference in resolved)
                {
                    _resolvedPackageReferences.TryAdd(reference.PackageName.ToLower(CultureInfo.InvariantCulture), reference);
                }

                packageRestoreResult =
                    new PackageRestoreResult(
                        succeeded: true,
                        requestedPackages: newlyRequested,
                        resolvedReferences: _resolvedPackageReferences
                                            .Values
                                            .Except(previouslyResolved)
                                            .ToList());
            }

            return packageRestoreResult;

            void ReportError(ErrorReportType errorType, int code, string message)
            {
                errors.Add($"PackageManagement {(errorType.IsError ? "Error" : "Warning")} {code} {message}");
            }
        }

        public void Dispose()
        {
            try
            {
                (_dependencies as IDisposable)?.Dispose();
                AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
            }
            catch
            {
            }
        }
    }
}
