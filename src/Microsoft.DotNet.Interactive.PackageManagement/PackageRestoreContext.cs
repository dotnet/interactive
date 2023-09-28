// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FSharp.Compiler.DependencyManager;
using Pocket;
using static Pocket.Logger;

namespace Microsoft.DotNet.Interactive.PackageManagement;

public class PackageRestoreContext : IDisposable
{
    private const string restoreTfm = "net7.0";
    private readonly ConcurrentDictionary<string, PackageReference> _requestedPackageReferences = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ResolvedPackageReference> _resolvedPackageReferences = new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, string> _requestedRestoreSources = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _resolvedRestoreSources = new(StringComparer.Ordinal);

    private readonly DependencyProvider _dependencyProvider;

    public PackageRestoreContext(bool forceRestore = false)
    {
        _dependencyProvider = new DependencyProvider(AssemblyProbingPaths, NativeProbingRoots, useResultsCache: !forceRestore);
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

    private IEnumerable<string> NativeProbingRoots()
    {
        foreach (var package in _resolvedPackageReferences.Values)
        {
            foreach (var path in package.ProbingPaths)
            {
                yield return path;
            }
        }
    }

    // By TryAdd we mean add it if it's not already in the collection
    public void TryAddRestoreSource(string source)
    {
        _resolvedRestoreSources.GetOrAdd(source, source);
    }

    public PackageReference GetOrAddPackageReference(
        string packageName,
        string packageVersion = null)
    {
        // Package names are case insensitive.
        var key = packageName.ToLower(CultureInfo.InvariantCulture);

        PackageReference resolvedPackage = null;
        if (_resolvedPackageReferences.TryGetValue(key, out var resolved))
        {
            if (string.IsNullOrWhiteSpace(packageVersion) ||
                packageVersion == "*" ||
                packageVersion == "*-*" ||
                string.Equals(resolved.PackageVersion.Trim(), packageVersion.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                resolvedPackage = resolved;
            }
            else
            {
                // Package already loaded with a different version
                return null;
            }
        }
        else
        {
            if (_requestedPackageReferences.TryGetValue(key, out var requested))
            {
                if (string.Equals(requested.PackageVersion.Trim(), packageVersion.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    resolvedPackage = requested;
                }
                else
                {
                    // Package already loaded with a different version
                    return null;
                }
            }
        }

        return AppendPathSeparator(resolvedPackage);

        PackageReference AppendPathSeparator(PackageReference resolved)
        {
            if (resolved is null)
            {
                var newPackageRef = new PackageReference(packageName, packageVersion);
                _requestedPackageReferences.TryAdd(key, newPackageRef);
                return newPackageRef;
            }
            else
            {
                var newPackageRef = new PackageReference(resolved.PackageName, resolved.PackageVersion);
                _requestedPackageReferences.TryAdd(key, newPackageRef);
                return newPackageRef;
            }
        }
    }

    public IEnumerable<string> RestoreSources => _requestedRestoreSources.Values.Concat(_resolvedRestoreSources.Values);

    public IEnumerable<PackageReference> RequestedPackageReferences => _requestedPackageReferences.Values;

    public IEnumerable<ResolvedPackageReference> ResolvedPackageReferences => _resolvedPackageReferences.Values;

    public ResolvedPackageReference GetResolvedPackageReference(string packageName) => _resolvedPackageReferences[packageName];

    private IEnumerable<Tuple<string, string>> GetPackageManagerLines()
    {
        // return restore sources
        foreach (var rs in RestoreSources)
        {
            yield return Tuple.Create("i", rs);
        }
        foreach (var pr in RequestedPackageReferences.Concat(ResolvedPackageReferences.Select(p => new PackageReference(p.PackageName, p.PackageVersion))).OrderBy(r => r.PackageName))
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

        if (!string.IsNullOrWhiteSpace(packageName) &&              // Name not empty
            !string.IsNullOrWhiteSpace(packageVersion) &&           // Version not empty
            char.IsDigit(packageVersion.Trim()[0]))                 // Version starts with a number
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
            catch (Exception ex)
            {
                Log.Error("Exception while trying to get package name and version {exception} for {packageName} and {packageVersion}",
                    ex, packageName, packageVersion);
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

    private IResolveDependenciesResult Resolve(
        IEnumerable<Tuple<string, string>> packageManagerTextLines,
        string executionTfm,
        ResolvingErrorReport reportError)
    {
        IDependencyManagerProvider iDependencyManager = _dependencyProvider.TryFindDependencyManagerByKey(Enumerable.Empty<string>(), "", reportError, "nuget");

        if (iDependencyManager is null)
        {
            // If this happens it is because of a bug in the Dependency provider. or deployment failed to deploy the nuget provider dll.
            // We guarantee the presence of the nuget provider, by shipping it with the interactive product
            throw new InvalidOperationException("Internal error - unable to locate the nuget package manager, please try to reinstall.");
        }

        return _dependencyProvider.Resolve(
            iDependencyManager,
            ".csx",
            packageManagerTextLines,
            reportError,
            executionTfm);
    }

    public async Task<PackageRestoreResult> RestoreAsync()
    {
        var newlyRequestedPackageReferences = _requestedPackageReferences
            .Select(r => r.Value)
            .Where(r => !_resolvedPackageReferences.ContainsKey(r.PackageName))
            .ToArray();

        var newlyRequestedRestoreSources = _requestedRestoreSources
            .Select(s => s.Value)
            .Where(s => !_resolvedRestoreSources.ContainsKey(s))
            .ToArray();

        var errors = new List<string>();

        var result = await Task.Run(() => Resolve(GetPackageManagerLines(), restoreTfm, ReportError));

        PackageRestoreResult packageRestoreResult;

        if (!result.Success)
        {
            errors.AddRange(result.StdOut);

            packageRestoreResult = new PackageRestoreResult(
                succeeded: false,
                requestedPackages: newlyRequestedPackageReferences,
                errors: errors);

            foreach (var r in newlyRequestedPackageReferences)
            {
                _requestedPackageReferences.TryRemove(r.PackageName, out _);
            }

            foreach (var s in newlyRequestedRestoreSources)
            {
                _requestedRestoreSources.TryRemove(s, out _);
            }
        }
        else
        {
            var previouslyResolved = _resolvedPackageReferences.Values.ToArray();

            var resolved = GetResolvedPackageReferences(result.Resolutions.Select(r => new FileInfo(r)),
                result.Roots.Select(r => new DirectoryInfo(r)));

            foreach (var reference in resolved)
            {
                _resolvedPackageReferences.TryAdd(reference.PackageName, reference);
                _requestedPackageReferences.TryRemove(reference.PackageName, out _);
            }

            foreach (var s in newlyRequestedRestoreSources)
            {
                _requestedRestoreSources.TryRemove(s, out _);
                _resolvedRestoreSources.TryAdd(s, s);
            }

            packageRestoreResult =
                new PackageRestoreResult(
                    succeeded: true,
                    requestedPackages: newlyRequestedPackageReferences,
                    resolvedReferences: _resolvedPackageReferences
                        .Values
                        .Except(previouslyResolved)
                        .ToImmutableArray());
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
            if (_dependencyProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
        }
        catch
        {
        }
    }
}