﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
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
using Microsoft.DotNet.DependencyManager;
using System.Runtime.InteropServices.ComTypes;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Microsoft.DotNet.Interactive
{
    public class PackageRestoreContext : IDisposable
    {
        private const string restoreTfm = "netcoreapp3.1";
        private const string packageKey = "nuget";
        private readonly ISupportNuget _iSupportNuget;
        private readonly ConcurrentDictionary<string, PackageReference> _requestedPackageReferences = new ConcurrentDictionary<string, PackageReference>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ResolvedPackageReference> _resolvedPackageReferences = new Dictionary<string, ResolvedPackageReference>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _restoreSources = new HashSet<string>();

        public PackageRestoreContext(ISupportNuget iSupportNuget)
        {
            _iSupportNuget = iSupportNuget;
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        internal IEnumerable<string> AssemblyProbingPaths()
        {
            foreach (var package in _resolvedPackageReferences.Values)
            {
                foreach (var fi in package.AssemblyPaths)
                    yield return fi.FullName;
            }
        }

        internal IEnumerable<string> NativeProbingRoots ()
        {
            foreach (var package in _resolvedPackageReferences.Values)
            {
                foreach (var di in package.ProbingPaths)
                {
                    yield return di.FullName;
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

        private IEnumerable<string> GetPackageManagerLines()
        {
            // return restore sources
            foreach( var rs in RestoreSources)
            {
                yield return $"RestoreSources={rs}";
            }
            foreach (var pr in RequestedPackageReferences)
            {
                yield return $"Include={pr.PackageName}, Version={pr.PackageVersion}";
            }
        }

        private bool TryGetPackageAndVersionFromPackageRoot(DirectoryInfo packageRoot, out PackageReference packageReference)
        {
            try
            {
                // packageRoot looks similar to:
                //    C:/Users/kevinr/.nuget/packages/fsharp.data/3.3.3/
                //    3.3.3 is the package version
                // fsharp.data is the package name
                var packageName = packageRoot.Parent.Name;
                var packageVersion = packageRoot.Name;
                if (_requestedPackageReferences.TryGetValue(packageName.ToLower(CultureInfo.InvariantCulture), out var requested))
                {
                    packageName = requested.PackageName;
                }
                packageReference = new PackageReference(packageName, packageVersion);
                return true;
            }
            catch(Exception)
            {
                packageReference = default(PackageReference);
                return false;
            }
        }

        private IEnumerable<FileInfo> GetAssemblyPathsForPackage(DirectoryInfo root, IEnumerable<FileInfo> resolutions)
        {
            foreach(var resolution in resolutions)
            {
                // Is the resolution within the package
                if(resolution.DirectoryName.StartsWith(root.FullName))
                    yield return resolution;
            }
        }

        private IEnumerable<ResolvedPackageReference> GetResolvedPackageReferences(
            IEnumerable<FileInfo> resolutions,
            IEnumerable<FileInfo> files,
            IEnumerable<DirectoryInfo> packageRoots)
        {
            foreach (var root in packageRoots)
            {
                if (TryGetPackageAndVersionFromPackageRoot(root, out var packageReference))
                {
                    var assemblyPaths = GetAssemblyPathsForPackage(root, resolutions);
                    var probingPaths = new List<DirectoryInfo>();
                    probingPaths.Add(root);

                    // PackageReference thingy
                    var resolvedPackageReference =
                        new ResolvedPackageReference(
                            packageReference.PackageName,
                            packageReference.PackageVersion,
                            new List<FileInfo>(assemblyPaths).AsReadOnly(),
                            root,
                            new List<DirectoryInfo>(probingPaths).AsReadOnly());
                    yield return resolvedPackageReference;
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

        public async Task<PackageRestoreResult> RestoreAsync()
        {
            var newlyRequested = RequestedPackageReferences
                                         .Where(r => !_resolvedPackageReferences.ContainsKey(r.PackageName.ToLower(CultureInfo.InvariantCulture)))
                                         .ToArray();

            var errors = new List<string>();

            ResolvingErrorReport ReportError = (ErrorReportType errorType, int code, string message) =>
            {
                errors.Add($"PackageManagement {(errorType.IsError ? "Error" : "Warning")} {code} {message}");
            };

            var result =
                await Task.Run(() => {
                    return _iSupportNuget.Resolve(GetPackageManagerLines(), restoreTfm, ReportError);
                });

            if (!result.Success)
            {
                errors.AddRange(result.StdOut);
                return new PackageRestoreResult(
                    succeeded: false,
                    requestedPackages: newlyRequested,
                    errors: errors);
            }
            else
            {
                var previouslyResolved = _resolvedPackageReferences.Values.ToArray();

                var resolved = GetResolvedPackageReferences(result.Resolutions.Select(r => new FileInfo(r)),
                                                            result.SourceFiles.Select(s => new FileInfo(s)),
                                                            result.Roots.Select(r => new DirectoryInfo(r)));

                foreach (var reference in resolved)
                {
                    _resolvedPackageReferences.TryAdd(reference.PackageName.ToLower(CultureInfo.InvariantCulture), reference);
                }

                var resolvedReferences = _resolvedPackageReferences
                                        .Values
                                        .Except(previouslyResolved)
                                        .ToList();
                return new PackageRestoreResult(
                    succeeded: true,
                    requestedPackages: newlyRequested,
                    resolvedReferences: _resolvedPackageReferences
                                        .Values
                                        .Except(previouslyResolved)
                                        .ToList());
            }
        }

        public void Dispose()
        {
            try
            {
                AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
            }
            catch
            {
            }
        }
    }
}