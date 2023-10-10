// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

internal class HttpBodyNode : HttpSyntaxNode
{
    internal HttpBodyNode(SourceText sourceText, HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public void Add(HttpEmbeddedExpressionNode node) => AddInternal(node);

    public HttpBindingResult<string> TryGetBody(HttpBindingDelegate bind)
    {
        return BindByInterpolation(bind);
    }
}