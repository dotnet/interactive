// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

internal class HttpHeadersNode : SyntaxNode
{
    internal HttpHeadersNode(
        SourceText sourceText,
        HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public IReadOnlyList<HttpHeaderNode> HeaderNodes => ChildNodes.OfType<HttpHeaderNode>().ToList();

    public void Add(HttpHeaderNode headerNode) => AddInternal(headerNode);

}