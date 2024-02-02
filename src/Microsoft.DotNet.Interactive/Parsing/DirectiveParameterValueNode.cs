// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class DirectiveParameterValueNode : SyntaxNode
{
    internal DirectiveParameterValueNode(
        SourceText sourceText,
        SyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public DirectiveExpressionTypeNode? ExpressionType { get; private set; }

    public DirectiveExpressionParametersNode? ExpressionParameters { get; private set; }

    public void Add(DirectiveExpressionTypeNode node)
    {
        AddInternal(node);
        ExpressionType = node;
    }

    public void Add(DirectiveExpressionParametersNode node)
    {
        AddInternal(node);
        ExpressionParameters = node;
    }
}