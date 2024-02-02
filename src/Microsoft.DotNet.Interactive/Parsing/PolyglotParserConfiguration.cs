// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class PolyglotParserConfiguration
{
    private Dictionary<string, KernelInfo>? _kernelInfoByKernelSpecifierDirectiveName;
    private HashSet<string>? _topLevelDirectives;

    public PolyglotParserConfiguration(string defaultKernelName = "")
    {
        DefaultKernelName = defaultKernelName ?? "";
    }

    public string DefaultKernelName { get; }

    public NamedSymbolCollection<KernelInfo> KernelInfos { get; } = new(info => info.LocalName);

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

        if (_kernelInfoByKernelSpecifierDirectiveName!.TryGetValue(currentKernelName, out var kernelInfo))
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

    public bool TryGetDirective(
        string currentKernelName,
        string directiveName,
        [MaybeNullWhen(false)] out KernelDirective directive)
    {
        EnsureSymbolMapIsInitialized();

        if (KernelInfos.TryGetValue(currentKernelName, out var kernelInfo))
        {
            if (kernelInfo.TryGetDirective(directiveName, out var foundDirective))
            {
                directive = foundDirective;
                return true;
            }
        }

        directive = null;
        return false;
    }

    public bool IsParameterInScope(DirectiveParameterNode namedParameter)
    {
        EnsureSymbolMapIsInitialized();

        if (namedParameter.GetKernelInfo() is { } kernelInfo &&
            namedParameter.Parent is DirectiveNode { DirectiveNameNode: { } directiveName })
        {
            if (kernelInfo.TryGetDirective(directiveName.Text, out _))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsKernelSelectorDirective(string text)
    {
        EnsureSymbolMapIsInitialized();

        return _kernelInfoByKernelSpecifierDirectiveName!.ContainsKey(text);
    }

    private void EnsureSymbolMapIsInitialized()
    {
        if (_kernelInfoByKernelSpecifierDirectiveName is null)
        {
            HashSet<string> topLevelDirectives = new();

            Dictionary<string, KernelInfo> dictionary = new();

            foreach (var kernelInfo in KernelInfos)
            {
                foreach (var tuple in kernelInfo.NameAndAliases.Select(alias => (alias, kernelInfo)))
                {
                    dictionary.Add("#!" + tuple.alias, tuple.kernelInfo);

                    foreach (var d in tuple.kernelInfo.SupportedDirectives.Where(d => d is not KernelSpecifierDirective))
                    {
                        topLevelDirectives.Add(d.Name);
                    }
                }
            }

            _kernelInfoByKernelSpecifierDirectiveName = dictionary;
            _topLevelDirectives = topLevelDirectives;
        }
    }
}