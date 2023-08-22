// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;
using System;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpVariableDeclarationAndAssignmentNode : HttpSyntaxNode
{
    public HttpVariableDeclarationNode? DeclarationNode { get; private set; }
    public HttpVariableAssignmentNode? AssignmentNode { get; private set; }
    public HttpExpressionNode? ExpressionNode { get; private set; } 

    internal HttpVariableDeclarationAndAssignmentNode(
        SourceText sourceText,
        HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }


    public void Add(HttpVariableDeclarationNode node)
    {
        if (DeclarationNode is not null)
        {
            throw new InvalidOperationException($"{nameof(DeclarationNode)} was already added.");
        }

        DeclarationNode = node;
        AddInternal(node);
    }

    public void Add(HttpVariableAssignmentNode node)
    {
        if (AssignmentNode is not null)
        {
            throw new InvalidOperationException($"{nameof(AssignmentNode)} was already added.");
        }

        AssignmentNode = node;
        AddInternal(node);
    }

    public void Add(HttpExpressionNode node)
    {
        if (ExpressionNode is not null)
        {
            throw new InvalidOperationException($"{nameof(ExpressionNode)} was already added.");
        }

        ExpressionNode = node;
        AddInternal(node);
    }
}
