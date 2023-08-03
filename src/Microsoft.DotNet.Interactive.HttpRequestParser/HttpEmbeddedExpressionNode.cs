// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpEmbeddedExpressionNode : HttpSyntaxNode
{
    internal HttpEmbeddedExpressionNode(
        SourceText sourceText,
        HttpSyntaxTree? syntaxTree,
        HttpExpressionStartNode startNode,
        HttpExpressionNode expressionNode,
        HttpExpressionEndNode endNode) : base(sourceText, syntaxTree)
    {
        StartNode = startNode;
        Add(StartNode);

        ExpressionNode = expressionNode;
        Add(ExpressionNode);

        EndNode = endNode;
        Add(EndNode);
    }

    public HttpExpressionStartNode StartNode { get; }
    public HttpExpressionNode ExpressionNode { get; }
    public HttpExpressionEndNode EndNode { get; }

}
