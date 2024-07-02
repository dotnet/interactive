// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

internal class HttpEmbeddedExpressionNode : HttpSyntaxNode
{
    internal HttpEmbeddedExpressionNode(
        SourceText sourceText,
        HttpSyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public HttpExpressionStartNode? StartNode { get; private set; }

    public HttpExpressionNode? ExpressionNode { get; private set; }

    public HttpExpressionEndNode? EndNode { get; private set; }

    public void Add(HttpExpressionStartNode startNode)
    {
        if (StartNode is not null)
        {
            throw new InvalidOperationException($"{nameof(StartNode)} was already added.");
        }

        StartNode = startNode;
        AddInternal(StartNode);
    }

    public void Add(HttpExpressionNode expressionNode)
    {
        if (ExpressionNode is not null)
        {
            throw new InvalidOperationException($"{nameof(ExpressionNode)} was already added.");
        }

        ExpressionNode = expressionNode;
        AddInternal(ExpressionNode);
    }

    public void Add(HttpExpressionEndNode endNode)
    {
        if (EndNode is not null)
        {
            throw new InvalidOperationException($"{nameof(EndNode)} was already added.");
        }

        EndNode = endNode;
        AddInternal(EndNode);
    }
}