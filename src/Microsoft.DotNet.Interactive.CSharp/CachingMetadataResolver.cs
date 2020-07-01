// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;


namespace Microsoft.DotNet.Interactive.CSharp
{
    internal sealed class CachingMetadataResolver : MetadataReferenceResolver, IEquatable<ScriptMetadataResolver>
    {
        private readonly ScriptMetadataResolver _resolver;
        public static CachingMetadataResolver Default { get; } = new CachingMetadataResolver(ImmutableArray<string>.Empty, null);

        private CachingMetadataResolver(ImmutableArray<string> searchPaths, string baseDirectoryOpt)
        {
            _resolver = ScriptMetadataResolver.Default
                .WithBaseDirectory(baseDirectoryOpt)
                .WithSearchPaths(searchPaths);
        }

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            return _resolver.ResolveReference(reference, baseFilePath, properties);
        }

        public override bool ResolveMissingAssemblies => _resolver.ResolveMissingAssemblies;

        private readonly ConcurrentDictionary<AssemblyIdentity, PortableExecutableReference> _resolvedAssembliesCache = new ConcurrentDictionary<AssemblyIdentity, PortableExecutableReference>();
        public override PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
        {
            return _resolvedAssembliesCache.GetOrAdd(referenceIdentity, id => _resolver.ResolveMissingAssembly(definition, id));
        }

        public CachingMetadataResolver WithBaseDirectory(string baseDirectory)
        {

            if (BaseDirectory == baseDirectory)
            {
                return this;
            }
            return new CachingMetadataResolver(SearchPaths, baseDirectory);
        }

        public ImmutableArray<string> SearchPaths => _resolver.SearchPaths;

        public string BaseDirectory => _resolver.BaseDirectory;

        public bool Equals(ScriptMetadataResolver other) => _resolver.Equals(other);

        public override bool Equals(object other) => Equals(other as ScriptMetadataResolver);

        public override int GetHashCode() => _resolver.GetHashCode();

    }
}