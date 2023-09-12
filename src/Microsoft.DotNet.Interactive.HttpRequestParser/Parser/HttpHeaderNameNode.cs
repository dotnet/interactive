// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.HttpRequest;

using Diagnostic = CodeAnalysis.Diagnostic;

internal class HttpHeaderNameNode : HttpSyntaxNode
{
    internal HttpHeaderNameNode(SourceText sourceText, HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public override IEnumerable<Diagnostic> GetDiagnostics()
    {
        foreach (var diagnostic in base.GetDiagnostics())
        {
            yield return diagnostic;
        }

        if (Span.Length == 0)
        {
            yield return CreateDiagnostic(WellKnownHttpDiagnostics.MissingHeaderName);
        }
        else if (TextContainsWhitespace())
        {
            yield return CreateDiagnostic(WellKnownHttpDiagnostics.InvalidWhitespaceInHeaderName);
        }
    }
}