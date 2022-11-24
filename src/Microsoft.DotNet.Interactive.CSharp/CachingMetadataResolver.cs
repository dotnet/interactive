// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace Microsoft.DotNet.Interactive.CSharp;

internal sealed class CachingMetadataResolver : MetadataReferenceResolver, IEquatable<ScriptMetadataResolver>
{
    private readonly ConcurrentDictionary<AssemblyIdentity, PortableExecutableReference> _resolvedAssembliesCache = new();
    private readonly ConcurrentDictionary<(string, string, MetadataReferenceProperties), ImmutableArray<PortableExecutableReference>> _xmlReferencesCache = new();

    private readonly ScriptMetadataResolver _resolver;
    private readonly string _baseDirectory;

    public static CachingMetadataResolver Default { get; } = new(ImmutableArray<string>.Empty, null);

    private CachingMetadataResolver(ImmutableArray<string> searchPaths, string baseDirectory)
    {
        _baseDirectory = baseDirectory;
            
        _resolver = ScriptMetadataResolver.Default
            .WithBaseDirectory(baseDirectory)
            .WithSearchPaths(searchPaths);
    }

    public override ImmutableArray<PortableExecutableReference> ResolveReference(
        string reference,
        string baseFilePath,
        MetadataReferenceProperties properties) =>
        _xmlReferencesCache.GetOrAdd((reference, baseFilePath, properties), t =>
        {
            var resolvedReferences = _resolver.ResolveReference(t.Item1, t.Item2, t.Item3);
            var xmlResolvedReferences = resolvedReferences.Select(r => ResolveReferenceWithXmlDocumentationProvider(r.FilePath, properties)).ToImmutableArray();

            return xmlResolvedReferences;
        });

    public override bool ResolveMissingAssemblies => _resolver.ResolveMissingAssemblies;

    public override PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity) => 
        _resolvedAssembliesCache.GetOrAdd(referenceIdentity, id => _resolver.ResolveMissingAssembly(definition, id));

    public CachingMetadataResolver WithBaseDirectory(string baseDirectory)
    {
        if (_baseDirectory == baseDirectory)
        {
            return this;
        }

        return new CachingMetadataResolver(SearchPaths, baseDirectory);
    }

    public ImmutableArray<string> SearchPaths => _resolver.SearchPaths;

    internal static PortableExecutableReference ResolveReferenceWithXmlDocumentationProvider(string path, MetadataReferenceProperties properties = default) => 
        MetadataReference.CreateFromFile(path, properties, XmlDocumentationProvider.CreateFromFile(Path.ChangeExtension(path, ".xml")));

    public bool Equals(ScriptMetadataResolver other) => _resolver.Equals(other);

    public override bool Equals(object other) => Equals(other as ScriptMetadataResolver);

    public override int GetHashCode() => _resolver.GetHashCode();
}