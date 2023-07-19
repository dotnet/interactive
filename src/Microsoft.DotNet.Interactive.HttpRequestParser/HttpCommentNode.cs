// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpCommentNode : HttpSyntaxNode
{
    internal HttpCommentNode(
        string sourceText,
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
    
}
