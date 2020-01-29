// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;
using Pocket;
using static Microsoft.DotNet.Interactive.Utility.PathUtilities;
using static Pocket.Logger;

namespace Microsoft.DotNet.Interactive
{
    public class NativeAssemblyLoadHelper : IDisposable
    {
        private static ImmutableArray<DirectoryInfo> _globalProbingDirectories = ImmutableArray<DirectoryInfo>.Empty;
        private ImmutableArray<DirectoryInfo> _probingDirectories = ImmutableArray<DirectoryInfo>.Empty;
        private readonly IKernel _kernel;

        private readonly ConcurrentDictionary<string, ResolvedPackageReference> _resolvedReferences =
            new ConcurrentDictionary<string, ResolvedPackageReference>(StringComparer.OrdinalIgnoreCase);

        public NativeAssemblyLoadHelper(IKernel kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        public void AddNativeLibraryProbingPaths(IReadOnlyList<DirectoryInfo> probingPaths)
        {
            _probingDirectories = _probingDirectories.AddRange(probingPaths).Distinct().ToImmutableArray();
            _globalProbingDirectories = _globalProbingDirectories.AddRange(probingPaths).Distinct().ToImmutableArray();
        }

        public void Handle(ResolvedPackageReference reference)
        {
            var assemblyFile = reference.AssemblyPaths.First();

            using var op = Log.OnEnterAndExit();

            if (_resolvedReferences.TryGetValue(assemblyFile.FullName, out var previous))
            {
                op.Info("Previously resolved {reference} at location {PackageRoot}", assemblyFile.FullName, previous.PackageRoot);
                return;
            }

            foreach (var dir in _probingDirectories)
            {
                if (IsSameDirectoryOrChildOf(
                    assemblyFile.FullName,
                    dir.FullName))
                {
                    op.Info("Resolved: {reference}", assemblyFile.FullName);
                    _resolvedReferences[assemblyFile.FullName] = reference;
                    return;
                }
            }
        }

        private IEnumerable<string> ProbingFilenames(string name)
        {
            // Try the name supplied by the pinvoke
            yield return name;

            if (!Path.IsPathRooted(name))
            {
                var usePrefix = ProbingUsePrefix();

                // Name is not rooted so we can try with prefix and suffix
                foreach (var suffix in ProbingSuffixes())
                {
                    if (ProbingUseSuffix(suffix))
                    {
                        yield return $"{name}{suffix}";

                        if (usePrefix)
                        {
                            yield return $"lib{name}{suffix}";
                        }
                    }
                    else
                    {
                        yield return $"lib{name}";
                    }
                }
            }

            // Probe for necessary suffixes --- these suffix' are platform specific
            IEnumerable<string> ProbingSuffixes()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    yield return ".dll";
                    yield return ".exe";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    yield return ".dylib";
                }
                else
                {
                    yield return ".so";
                }
            }

            // linux developers often append a version number to libraries I.e mydll.so.5.3.2
            bool ProbingUseSuffix(string s)
            {
                return !(name.Contains(s + ".") || name.EndsWith(s));
            }

            // If the name looks like a path or a volume name then dont prefix 'lib'
            bool ProbingUsePrefix()
            {
                return name.IndexOf(Path.DirectorySeparatorChar) == -1
                       && name.IndexOf(Path.AltDirectorySeparatorChar) == -1
                       && name.IndexOf(Path.PathSeparator) == -1
                       && name.IndexOf(Path.VolumeSeparatorChar) == -1;
            }
        }

        private IEnumerable<string> FilesMatchingLibName(string probingPath, string name)
        {
            // if name is rooted then it's an absolute path to the dll
            if (Path.IsPathRooted(name))
            {
                if (File.Exists(name))
                {
                    yield return name;
                }
            }
            else
            {
                // Check if the dll exists in the probe path root
                foreach(var pname in ProbingFilenames(name))
                {
                    var path = Path.Combine(probingPath, pname);
                    if (File.Exists(path))
                    {
                        yield return path;
                    }
                }
                // Grovel through the platform specific subdirectory
                foreach(var rid in ProbingRids())
                {
                    var path = Path.Combine(probingPath, "runtimes", rid, "native");

                    // Check if the dll exists in the rid specific native directory
                    foreach (var pname in ProbingFilenames(name))
                    {
                        var p = Path.Combine(path, pname);
                        
                        if (File.Exists(p))
                        {
                            yield return p;
                        }
                    }
                }
            }

            // Computer valid dotnet-rids for this environment: https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
            // Where rid is: win, win-x64, win-x86, osx-x64, linux-x64 etc ...
            IEnumerable<string> ProbingRids()
            {
                var processArchitecture = RuntimeInformation.ProcessArchitecture;
                var baseRid =
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" :
                    "linux";

                var platformRid =
                    processArchitecture == Architecture.X64 ? "-x64" :
                    processArchitecture == Architecture.X86 ? "-x86" :
                    processArchitecture == Architecture.Arm64 ? "-arm64" :
                    "arm";

                yield return baseRid + platformRid;
                yield return baseRid;
                yield return "any";
            }
        }

        private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (args.LoadedAssembly.IsDynamic ||
                string.IsNullOrWhiteSpace(args.LoadedAssembly.Location))
            {
                return;
            }

            if (KernelInvocationContext.Current?.HandlingKernel != _kernel)
            {
                return;
            }

            Log.Info("OnAssemblyLoad: {location}", args.LoadedAssembly.Location);
            
            NativeLibrary.SetDllImportResolver(
                args.LoadedAssembly,
                Resolve());

            DllImportResolver Resolve()
            {
                return (libraryName, assembly, searchPath) =>
                {
                    var ptr = IntPtr.Zero;

                    if (_resolvedReferences.TryGetValue(
                        args.LoadedAssembly.Location,
                        out var reference))
                    {
                        ptr = _probingDirectories
                              .Where(p => IsSameDirectoryOrChildOf(
                                         p.FullName,
                                         reference.PackageRoot.FullName))
                              .SelectMany(
                                  dir => FilesMatchingLibName(dir.FullName, libraryName)
                                      .Select(LoadNative))
                              .FirstOrDefault(p => p != default);
                    }

                    if (ptr == IntPtr.Zero)
                    {
                        ptr = _globalProbingDirectories
                              .SelectMany(
                                  dir => FilesMatchingLibName(dir.FullName, libraryName)
                                      .Select(LoadNative))
                              .FirstOrDefault(p => p != default);
                    }

                    return ptr;

                    IntPtr LoadNative(string dll)
                    {
                        try
                        {
                            if (Interlocked.Increment(ref _recursionCount) == 1)
                            {
                                ptr = NativeLibrary.Load(dll);
                                Log.Info("NativeLibrary.Load({dll})", args.LoadedAssembly.Location);
                            }
                        }
                        catch
                        {
                        }
                        finally
                        {
                            Interlocked.Decrement(ref _recursionCount);
                        }

                        return ptr;
                    }
                };
            }
        }

        private static int _recursionCount = 0;

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
        }
    }
}