// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.HttpRequest;


/*

Rough sketch, probably out of date:
 
HttpRootSyntaxNode
|-HttpVariableDeclarationNode
  |--VariablePrefixToken
  |--VariableNameToken
  |--VariableAssignmentOperatorToken
  |--ValueExpressionNode
|--HttpRequestNode
  |--HttpMethodToken
  |--HttpUrlNode
     |--HttpUrlSchemeToken
     |--HttpUrlHostToken
     |--HttpUrlQueryToken
  |--HttpRequestBodyNode

 */

internal class HttpSyntaxTree
{
    private readonly string _sourceText;

    public HttpSyntaxTree(string sourceText)
    {
        _sourceText = sourceText;
    }

    public HttpRootSyntaxNode RootNode { get; set; }
}