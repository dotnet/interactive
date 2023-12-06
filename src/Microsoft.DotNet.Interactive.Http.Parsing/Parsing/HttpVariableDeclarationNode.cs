// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

using Diagnostic = CodeAnalysis.Diagnostic;

internal class HttpVariableDeclarationNode : SyntaxNode
{
    internal HttpVariableDeclarationNode(SourceText sourceText, HttpSyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public string VariableName => Text.Substring(1);

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
