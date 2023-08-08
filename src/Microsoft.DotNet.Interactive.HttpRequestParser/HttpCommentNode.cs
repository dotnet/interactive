// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpCommentNode : HttpSyntaxNode
{
    internal HttpCommentNode(
        SourceText sourceText,
        HttpSyntaxTree? syntaxTree,
        HttpCommentStartNode commentStartNode,
        HttpCommentBodyNode? commentBodyNode) : base(sourceText, syntaxTree)
    {
        CommentStartNode = commentStartNode;
        Add(CommentStartNode);
        
        if(commentBodyNode is not null)
        {
            CommentBodyNode = commentBodyNode;
            Add(CommentBodyNode);
        }
        
    }

    public HttpCommentStartNode CommentStartNode { get; }

    public HttpCommentBodyNode? CommentBodyNode { get; }

    public override bool IsSignificant => false;
}
