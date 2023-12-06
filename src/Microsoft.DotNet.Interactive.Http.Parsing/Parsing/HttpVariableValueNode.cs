// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

internal class HttpVariableValueNode : SyntaxNode
{
    internal HttpVariableValueNode(SourceText sourceText, HttpSyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public void Add(HttpEmbeddedExpressionNode node) => AddInternal(node);

    public HttpBindingResult<string> TryGetValue(HttpBindingDelegate bind) => this.BindByInterpolation(bind);
}