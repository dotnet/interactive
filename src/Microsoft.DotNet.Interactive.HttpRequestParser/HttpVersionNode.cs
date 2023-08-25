// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpVersionNode : HttpSyntaxNode
{
    internal HttpVersionNode(SourceText sourceText, HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public override IEnumerable<Diagnostic> GetDiagnostics()
    {
        foreach (var diagnostic in base.GetDiagnostics())
        {
            yield return diagnostic;
        }

        if (ChildTokens.FirstOrDefault() is { Kind: HttpTokenKind.Word } word)
        {
            if (word.Text.ToLowerInvariant() is not "http" and not "https")
            {
                yield return CreateDiagnostic("Invalid HTTP version");
            }

            if (ChildNodesAndTokens.OfType<HttpSyntaxToken>().Any(t => t.Kind is HttpTokenKind.Whitespace))
            {
                yield return CreateDiagnostic("Invalid HTTP version");
            }
        }
    }
}