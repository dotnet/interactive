// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Http.Parsing.Parsing;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

internal class HttpRootSyntaxNode : HttpSyntaxNode
{
    internal HttpRootSyntaxNode(SourceText sourceText, HttpSyntaxTree? tree) : base(sourceText, tree)
    {
    }

    public void Add(HttpRequestNode requestNode)
    {
        AddInternal(requestNode);
    }

    public void Add(HttpCommentNode commentNode)
    {
        AddInternal(commentNode);
    }

    public void Add(HttpVariableDeclarationAndAssignmentNode variableNode)
    {
        AddInternal(variableNode);
    }

    public void Add(HttpRequestSeparatorNode separatorNode)
    {
        AddInternal(separatorNode);
    }

    public Dictionary<string, DeclaredVariable> GetDeclaredVariables()
    {

        var variableAndDeclarationNodes = ChildNodes.OfType<HttpVariableDeclarationAndAssignmentNode>();

        var foundVariableValues = new Dictionary<string, string>();
        var declaredVariables = new Dictionary<string, DeclaredVariable>();

        foreach (var node in variableAndDeclarationNodes)
        {
            if (node.ValueNode is not null && node.DeclarationNode is not null)
            {
                var embeddedExpressionNodes = node.ValueNode.ChildNodes.OfType<HttpEmbeddedExpressionNode>();
                if (!embeddedExpressionNodes.Any())
                {
                    foundVariableValues.Add(node.DeclarationNode.VariableName, node.ValueNode.Text);
                    declaredVariables[node.DeclarationNode.VariableName] = new DeclaredVariable(node.DeclarationNode.VariableName, node.ValueNode.Text, HttpBindingResult<string>.Success(Text));
                }
                else
                {
                    var value = node.ValueNode.TryGetValue(node =>
                    {
                        if (foundVariableValues.TryGetValue(node.Text, out string? strinValue))
                        {
                            return node.CreateBindingSuccess(strinValue);
                        }
                        else
                        {
                            return DynamicExpressionUtilities.ResolveExpressionBinding(node, node.Text);
                        }

                    });

                    if (value is not null && value.Value is not null)
                    {
                        declaredVariables[node.DeclarationNode.VariableName] = new DeclaredVariable(node.DeclarationNode.VariableName, value.Value, value);
                    }
                }
            }
        }

        return declaredVariables;
    }
}