// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class DirectiveExpressionNode : SyntaxNode
{
    internal DirectiveExpressionNode(SourceText sourceText, SyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public DirectiveExpressionTypeNode? TypeNode { get; private set; }

    public DirectiveExpressionParametersNode? ParametersNode { get; private set; }

    public bool IsInputExpression => 
        TypeNode?.Type is "input" or "password";

    public void Add(DirectiveExpressionTypeNode node)
    {
        AddInternal(node);
        TypeNode = node;
    }

    public void Add(DirectiveExpressionParametersNode node)
    {
        AddInternal(node);
        ParametersNode = node;
    }
}