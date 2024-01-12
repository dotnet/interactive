// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

using Diagnostic = CodeAnalysis.Diagnostic;

internal class HttpVariableDeclarationNode : HttpSyntaxNode
{
    internal HttpVariableDeclarationNode(SourceText sourceText, HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public string VariableName => string.Join("", ChildTokens.Where(t => t.Kind == HttpTokenKind.Word || (t.Kind == HttpTokenKind.Punctuation && t.Text is "_")).Select(t => t.Text)) ?? string.Empty;

    public override IEnumerable<Diagnostic> GetDiagnostics()
    {
        foreach (var diagnostic in base.GetDiagnostics())
        {
            yield return diagnostic;
        }

        if (string.IsNullOrWhiteSpace(VariableName))
        {
            yield return CreateDiagnostic(HttpDiagnostics.VariableNameExpected());
        }
    }
}
