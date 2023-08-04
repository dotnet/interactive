// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpUrlNode : HttpSyntaxNode
{
    internal HttpUrlNode(SourceText sourceText, HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }
    
    internal HttpBindingResult<Uri> TryGetUri(HttpBindingDelegate bind)
    {
        var urlText = new StringBuilder();

        var diagnostics = new List<Diagnostic>();
        var success = true;

        foreach (var node in ChildNodesAndTokens)
        {
            if (node is HttpEmbeddedExpressionNode n)
            {
                var innerResult = bind(n.ExpressionNode);

                if (!innerResult.IsSuccessful)
                {
                    success = false;
                }
                else
                {
                    var nodeText = innerResult.Value?.ToString();
                    urlText.Append(nodeText);
                }

                diagnostics.AddRange(innerResult.Diagnostics);
            }
            else
            {
                urlText.Append(node.Text);
            }
        }

        if (success)
        {
            var uri = new Uri(urlText.ToString(), UriKind.Absolute);

            return HttpBindingResult<Uri>.Success(uri);
        }
        else
        {
            return HttpBindingResult<Uri>.Failure(diagnostics.ToArray());
        }
    }
}