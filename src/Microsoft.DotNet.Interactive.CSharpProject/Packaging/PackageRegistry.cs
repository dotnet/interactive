// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging;

public class PackageRegistry :
    IPackageFinder,
    IEnumerable<Task<PackageBuilder>>
{
    private readonly List<IPackageFinder> _packageFinders;

    private readonly ConcurrentDictionary<string, Task<PackageBuilder>> _packageBuilders = new ConcurrentDictionary<string, Task<PackageBuilder>>();
    private readonly ConcurrentDictionary<string, Task<IPackage>> _packages = new ConcurrentDictionary<string, Task<IPackage>>();
    private readonly ConcurrentDictionary<PackageDescriptor, Task<IPackage>> _packages2 = new ConcurrentDictionary<PackageDescriptor, Task<IPackage>>();
    private readonly List<IPackageDiscoveryStrategy> _strategies = new List<IPackageDiscoveryStrategy>();

    public PackageRegistry(
        bool createRebuildablePackages = false,
        PackageSource addSource = null,
        IEnumerable<IPackageFinder> packageFinders = null,
        params IPackageDiscoveryStrategy[] additionalStrategies)
        : this(addSource,
            new IPackageDiscoveryStrategy[]
            {
                new ProjectFilePackageDiscoveryStrategy(createRebuildablePackages),
                new DirectoryPackageDiscoveryStrategy(createRebuildablePackages)
            }.Concat(additionalStrategies),
            packageFinders)
    {
    }

    private PackageRegistry(
        PackageSource addSource,
        IEnumerable<IPackageDiscoveryStrategy> strategies,
        IEnumerable<IPackageFinder> packageFinders = null)
    {
        foreach (var strategy in strategies)
        {
            if (strategy == null)
            {
                throw new ArgumentException("Strategy cannot be null.");
            }

            _strategies.Add(strategy);
        }

        _packageFinders = packageFinders?.ToList() ?? GetDefaultPackageFinders().ToList();
    }

    public void Add(string name, Action<PackageBuilder> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        var packageBuilder = new PackageBuilder(name);
        configure(packageBuilder);
        _packageBuilders.TryAdd(name, Task.FromResult(packageBuilder));
    }

    public async Task<T> Get<T>(string packageName)
        where T : class, IPackage
    {
        if (packageName == "script")
        {
            packageName = "console";
        }

        var descriptor = new PackageDescriptor(packageName);

        var package = await GetPackage2<T>(descriptor);

        if (!(package is T))
        {
            package = await GetPackageFromPackageBuilder(packageName, descriptor);
        }

        return (T)package;
    }

    private async Task<IPackage> GetPackage2<T>(PackageDescriptor descriptor)
        where T : class, IPackage
    {
        var package = await ( _packages2.GetOrAdd(descriptor, async descriptor2 =>
        {
            foreach (var packageFinder in _packageFinders)
            {
                var package2 = await packageFinder.Find<Package2>(descriptor);
                if (package2 != null)
                {
                    return package2;
                }
            }

            return default;
        }));

        if (package != null)
        {
            if (package is T pkg)
            {
                return pkg;
            }

            if (package is Package2 package2)
            {
                var packageAsset = package2.Assets.OfType<T>().FirstOrDefault();
                if (packageAsset != null)
                {
                    return packageAsset;
                }
            }
        }

        return null;
    }

    private Task<IPackage> GetPackageFromPackageBuilder(string packageName, PackageDescriptor descriptor)
    {
        return _packages.GetOrAdd(packageName, async name =>
        {
            var packageBuilder = await _packageBuilders.GetOrAdd(
                name,
                async name2 =>
                {
                    foreach (var strategy in _strategies)
                    {
                        var builder = await strategy.LocatePackageAsync(descriptor);

                        if (builder != null)
                        {
                            return builder;
                        }
                    }

                    throw new PackageNotFoundException($"Package named \"{name2}\" not found.");
                });

            return packageBuilder.GetPackage();
        });
    }

    public static PackageRegistry CreateForHostedMode()
    {
        var finders = GetDefaultPackageFinders();
        var registry = new PackageRegistry(
            createRebuildablePackages: false,
            packageFinders: finders);

        return registry;
    }

    public IEnumerator<Task<PackageBuilder>> GetEnumerator() =>
        _packageBuilders.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    private static IEnumerable<IPackageFinder> GetDefaultPackageFinders()
    {
        yield return new PackageNameIsFullyQualifiedPath();
        yield return new FindPackageInDefaultLocation(new FileSystemDirectoryAccessor(Package.DefaultPackagesDirectory));
    }

    Task<T> IPackageFinder.Find<T>(PackageDescriptor descriptor)
    {
        return Get<T>(descriptor.Name);
    }
}