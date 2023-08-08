// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpBodyNode : HttpSyntaxNode
{
    internal HttpBodyNode(SourceText sourceText, HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public HttpBindingResult<string> TryGetBody(Func<HttpExpressionNode, HttpBindingResult<object?>> bind)
    {
        var bodyText = new StringBuilder();
        var diagnostics = new List<Diagnostic>();
        var success = true;

        foreach (var node in ChildNodesAndTokens)
        {
            if (node is HttpEmbeddedExpressionNode n)
            {
                var innerResult = bind(n.ExpressionNode);

                if (innerResult.IsSuccessful)
                {
                    var nodeText = innerResult.Value?.ToString();
                    bodyText.Append(nodeText);
                }
                else
                {
                    success = false;
                }

                diagnostics.AddRange(innerResult.Diagnostics);
            }
            else
            {
                bodyText.Append(node.TextWithTrivia);
            }
        }

        if (success)
        {
            return HttpBindingResult<string>.Success(bodyText.ToString());
        }
        else
        {
            return HttpBindingResult<string>.Failure(diagnostics.ToArray());
        }
    }
}