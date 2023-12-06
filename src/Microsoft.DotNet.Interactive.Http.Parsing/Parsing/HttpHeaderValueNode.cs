// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

using Diagnostic = CodeAnalysis.Diagnostic;

internal class HttpHeaderValueNode : SyntaxNode
{
    internal HttpHeaderValueNode(SourceText sourceText, HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public void Add(HttpEmbeddedExpressionNode node) => AddInternal(node);

    public HttpBindingResult<string> TryGetValue(HttpBindingDelegate bind)
    {
        return BindByInterpolation(bind);
    }

    public override IEnumerable<Diagnostic> GetDiagnostics()
    {
        foreach (var diagnostic in base.GetDiagnostics())
        {
            yield return diagnostic;
        }

        if (Span.Length == 0)
        {
            yield return CreateDiagnostic(HttpDiagnostics.MissingHeaderValue());
        }
    }
}