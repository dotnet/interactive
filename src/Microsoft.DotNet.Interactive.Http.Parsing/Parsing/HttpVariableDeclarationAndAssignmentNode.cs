// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;
using System;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

internal class HttpVariableDeclarationAndAssignmentNode : HttpSyntaxNode
{
    public HttpVariableDeclarationNode? DeclarationNode { get; private set; }
    public HttpVariableAssignmentNode? AssignmentNode { get; private set; }
    public HttpVariableValueNode? ValueNode { get; private set; } 

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

    public void Add(HttpVariableValueNode node)
    {
        if (ValueNode is not null)
        {
            throw new InvalidOperationException($"{nameof(ValueNode)} was already added.");
        }

        ValueNode = node;
        AddInternal(node);
    }
}
