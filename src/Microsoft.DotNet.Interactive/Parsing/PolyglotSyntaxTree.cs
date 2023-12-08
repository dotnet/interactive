// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class PolyglotSyntaxTree : SyntaxTree
{
    internal PolyglotSyntaxTree(SourceText sourceText, string defaultLanguage)
    {
        DefaultLanguage = defaultLanguage;
        RootNode = new PolyglotSubmissionNode(sourceText, this);
    }

    public string DefaultLanguage { get; }

    public PolyglotSubmissionNode RootNode { get; }

    public string? GetLanguageAtPosition(int position)
    {
        if (position >= RootNode.Span.End)
        {
            position = RootNode.Span.End - 1;
        }

        var node = RootNode.FindNode(position);

        switch (node)
        {
            case LanguageNode languageNode:
                return languageNode.Name;

            case PolyglotSubmissionNode submissionNode:
                return DefaultLanguage;

            default:
                return null;
        }
    }
}
