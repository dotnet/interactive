// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class PolyglotSyntaxTree : SyntaxTree
{
    internal PolyglotSyntaxTree(SourceText sourceText, PolyglotParserConfiguration parserConfiguration)
    {
        ParserConfiguration = parserConfiguration;
        RootNode = new PolyglotSubmissionNode(sourceText, this);
    }

    public PolyglotParserConfiguration ParserConfiguration { get; }

    public SyntaxNode? RootNode
    {
        get => _root;
        set => _root = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override string ToString()
    {
        return _sourceText.ToString();
    }

    public string? GetLanguageAtPosition(int position)
    {
        if (_root is null)
        {
            position = RootNode.Span.End - 1;
        }

        var node = RootNode.FindNode(position);

        var node = _root.FindNode(position);

        switch (node)
        {
            case LanguageNode languageNode:
                return languageNode.TargetKernelName;
                
            default:
                return null;
        }
    }
}

