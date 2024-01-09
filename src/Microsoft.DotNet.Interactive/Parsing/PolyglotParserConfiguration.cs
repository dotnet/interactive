// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class PolyglotParserConfiguration
{
    private Dictionary<string, KernelInfo>? _kernelInfoByKernelName;
    private HashSet<string>? _topLevelDirectives;

    public Dictionary<string, KernelInfo> KernelInfos { get; } = new();

    public bool IsDirectiveInScope(
        string currentKernelName, 
        string directiveName, 
        [NotNullWhen(true)] out DirectiveNodeKind? kind)
    {
        EnsureKernelInfoMapIsInitialized();

        if (IsKernelSelectorDirective(directiveName))
        {
            kind = DirectiveNodeKind.KernelSelector;
            return true;
        }

        if (_topLevelDirectives!.Contains(directiveName))
        {
            kind = DirectiveNodeKind.Action;
            return true;
        }

        if (_kernelInfoByKernelName!.TryGetValue(currentKernelName, out var kernelInfo))
        {
            if (kernelInfo.SupportedDirectives.SingleOrDefault(d => d.Name == directiveName) is { } directive)
            {
                if (directive.IsKernelSpecifier)
                {
                    kind = DirectiveNodeKind.KernelSelector;
                }
                else
                {
                    kind = DirectiveNodeKind.Action;
                }

                return true;
            }
        }

        kind = null;
        return false;
    }

    public bool IsKernelSelectorDirective(string text)
    {
        EnsureKernelInfoMapIsInitialized();

        return _kernelInfoByKernelName!.ContainsKey(text);
    }

    private void EnsureKernelInfoMapIsInitialized()
    {
        HashSet<string> topLevelDirectives = new();

        if (_kernelInfoByKernelName is null)
        {
            Dictionary<string, KernelInfo> dictionary = new();

            foreach (var pair in KernelInfos)
            {
                foreach (var tuple in pair.Value.NameAndAliases.Select(alias => (alias, pair.Value)))
                {
                    dictionary.Add("#!" + tuple.alias, tuple.Value);

                    foreach (var d in tuple.Value.SupportedDirectives.Where(d => !d.IsKernelSpecifier))
                    {
                        topLevelDirectives.Add(d.Name);
                    }
                }
            }

            _kernelInfoByKernelName = dictionary;
            _topLevelDirectives = topLevelDirectives;
        }
    }
}