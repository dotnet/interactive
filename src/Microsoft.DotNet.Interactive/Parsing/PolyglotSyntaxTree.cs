// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

public class PolyglotSyntaxTree
{
    private readonly SourceText _sourceText;
    private SyntaxNode? _root;

    internal PolyglotSyntaxTree(SourceText sourceText)
    {
        _sourceText = sourceText;
    }

    public int Length => _sourceText.Length;

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
            return null;
        }

        if (position >= _root.Span.End)
        {
            position = _root.Span.End - 1;
        }

        var node = _root.FindNode(position);

        switch (node)
        {
            case LanguageNode languageNode:
                return languageNode.Name;

            case PolyglotSubmissionNode submissionNode:
                return submissionNode.DefaultLanguage;

            default:
                return null;
        }
    }

    public int GetAbsolutePosition(LinePosition linePosition)
    {
        return _sourceText.Lines.GetPosition(linePosition.ToCodeAnalysisLinePosition());
    }
}