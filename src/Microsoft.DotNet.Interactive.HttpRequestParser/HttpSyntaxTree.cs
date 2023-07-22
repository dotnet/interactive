// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;

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
    private readonly SourceText _sourceText;

    public HttpSyntaxTree(SourceText sourceText)
        => _sourceText = sourceText;

    public HttpRootSyntaxNode? RootNode { get; set; }
}
