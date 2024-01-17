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

    public PolyglotParserConfiguration(string defaultKernelName = "")
    {
        DefaultKernelName = defaultKernelName ?? "";
    }

    public string DefaultKernelName { get; }

    public Dictionary<string, KernelInfo> KernelInfos { get; } = new();

    public bool IsDirectiveInScope(
        string currentKernelName,
        string directiveName,
        [NotNullWhen(true)] out DirectiveNodeKind? kind)
    {
        EnsureSymbolMapIsInitialized();

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
                if (directive is KernelSpecifierDirective)
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

    public bool IsOptionInScope(DirectiveOptionNode option)
    {
        EnsureSymbolMapIsInitialized();

        if (option.Parent is DirectiveNode directive &&
            directive.ChildNodes.OfType<DirectiveNameNode>().SingleOrDefault() is { } directiveName)
        {
           



        }

        return false;
    }

    public bool IsKernelSelectorDirective(string text)
    {
        EnsureSymbolMapIsInitialized();

        return _kernelInfoByKernelName!.ContainsKey(text);
    }

    private void EnsureSymbolMapIsInitialized()
    {
        if (_kernelInfoByKernelName is null)
        {
            HashSet<string> topLevelDirectives = new();

            Dictionary<string, KernelInfo> dictionary = new();

            foreach (var pair in KernelInfos)
            {
                foreach (var tuple in pair.Value.NameAndAliases.Select(alias => (alias, pair.Value)))
                {
                    dictionary.Add("#!" + tuple.alias, tuple.Value);

                    foreach (var d in tuple.Value.SupportedDirectives.Where(d => d is not KernelSpecifierDirective))
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