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

    /*internal bool TryGetUri(BindingDelegate bind, out Uri uri)
    {
        var urlText = new StringBuilder();
        
        foreach(var node in ChildNodesAndTokens)
        {
            urlText.Append(node switch
            {
                HttpEmbeddedExpressionNode n => bind(n.ExpressionNode),
                _ => node.Text
            });
        }
        var uri = new Uri(urlText.ToString(), UriKind.Absolute);
    }*/
}

internal delegate bool BindingDelegate(HttpExpressionNode name, BindingContext context, out object? value);

internal class BindingContext
{

}