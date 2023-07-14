// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpHeadersNode : HttpSyntaxNode
{
    internal HttpHeadersNode(
        string sourceText,
        HttpSyntaxTree? syntaxTree,
        IReadOnlyList<HttpHeaderNode> headerNodes) : base(sourceText, syntaxTree)
    {
        HeaderNodes = headerNodes;
        foreach (var headerNode in HeaderNodes)
        {
            Add(headerNode);
        }
    }

    public IReadOnlyList<HttpHeaderNode> HeaderNodes { get; }
}
