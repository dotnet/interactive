// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

internal class HttpSyntaxNode : SyntaxNode
{
    internal HttpSyntaxNode(SourceText sourceText, HttpSyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    internal void Add(HttpCommentNode node, bool addBefore) => AddInternal(node, addBefore);

    protected bool TextContainsWhitespace()
    {
        // We ignore whitespace if it's the first or last token, OR ignore the first or last token if it's not whitespace.  For this reason, the first and last tokens aren't interesting.
        for (var i = 1; i < ChildNodesAndTokens.Count - 1; i++)
        {
            var nodeOrToken = ChildNodesAndTokens[i];
            if (nodeOrToken is SyntaxToken { Kind: TokenKind.Whitespace })
            {
                if (nodeOrToken.Span.OverlapsWith(Span))
                {
                    return true;
                }
            }
        }

        return false;
    }
}