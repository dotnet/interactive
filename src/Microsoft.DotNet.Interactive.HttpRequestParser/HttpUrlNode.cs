// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;
using System;
using System.Text;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpUrlNode : HttpSyntaxNode
{
    internal HttpUrlNode(SourceText sourceText, HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    internal Uri GetUri(Func<HttpExpressionNode, object> value)
    {
        var urlText = new StringBuilder();
        
        foreach(var node in ChildNodesAndTokens)
        {
            urlText.Append(node switch
            {
                HttpEmbeddedExpressionNode n => value(n.ExpressionNode),
                _ => node.Text
            });
        }
        return new Uri(urlText.ToString(), UriKind.Absolute);
    }
}
