// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpRootSyntaxNode : HttpSyntaxNode
{
    public HttpRootSyntaxNode(string sourceText, HttpSyntaxTree tree) :
        base(sourceText, tree)
    {
    }
}