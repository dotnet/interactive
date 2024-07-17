// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

internal class HttpCommentNode : HttpSyntaxNode
{
    internal HttpCommentNode(
        SourceText sourceText,
        HttpSyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public HttpCommentStartNode? CommentStartNode { get; private set; }

    public HttpCommentBodyNode? CommentBodyNode { get; private set; }

    public HttpNamedRequestNode? NamedRequestNode { get; private set; }

    public override bool IsSignificant => false;

    public void Add(HttpCommentStartNode node)
    {
        if (CommentStartNode is not null)
        {
            throw new InvalidOperationException($"{nameof(CommentStartNode)} was already added.");
        }

        CommentStartNode = node;
        AddInternal(node);
    }

    public void Add(HttpCommentBodyNode node)
    {
        if (CommentBodyNode is not null)
        {
            throw new InvalidOperationException($"{nameof(CommentBodyNode)} was already added.");
        }

        CommentBodyNode = node;
        AddInternal(node);
    }

    public void Add(HttpNamedRequestNode node)
    {
        if (NamedRequestNode is not null)
        {
            throw new InvalidOperationException($"{nameof(NamedRequestNode)} was already added.");
        }

        NamedRequestNode = node;
        AddInternal(node);
    }
}