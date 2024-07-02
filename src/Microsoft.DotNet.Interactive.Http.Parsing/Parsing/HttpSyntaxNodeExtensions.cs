// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

internal static class HttpSyntaxNodeExtensions
{
    public static HttpBindingResult<string> BindByInterpolation(
        this HttpSyntaxNode httpSyntaxNode, 
        HttpBindingDelegate bind)
    {
        var text = new StringBuilder();
        var diagnostics = new List<CodeAnalysis.Diagnostic>();
        var success = true;

        foreach (var node in httpSyntaxNode.ChildNodesAndTokens)
        {
            if (node is HttpEmbeddedExpressionNode { ExpressionNode: not null } n)
            {
                var innerResult = bind(n.ExpressionNode);

                if (innerResult.IsSuccessful)
                {
                    var nodeText = innerResult.Value?.ToString();
                    text.Append(nodeText);
                }
                else
                {
                    success = false;
                }

                diagnostics.AddRange(innerResult.Diagnostics);
            }
            else
            {
                text.Append(node.Text);
            }
        }

        if (success)
        {
            return HttpBindingResult<string>.Success(text.ToString().Trim(), diagnostics.ToArray());
        }
        else
        {
            return HttpBindingResult<string>.Failure(diagnostics.ToArray());
        }
    }
}