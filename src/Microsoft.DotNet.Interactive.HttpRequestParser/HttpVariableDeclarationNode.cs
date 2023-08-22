// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal class HttpVariableDeclarationNode : HttpSyntaxNode
{
    internal HttpVariableDeclarationNode(SourceText sourceText, HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public string VariableName => ChildTokens.Where(t => t.Kind == HttpTokenKind.Word).Select(t => t.Text).FirstOrDefault() ?? string.Empty;

   
    public override IEnumerable<Diagnostic> GetDiagnostics()
    {
        foreach (var diagnostic in base.GetDiagnostics())
        {
            yield return diagnostic;
        }

        yield return base.CreateDiagnostic("Variable name expected.");
    }
}
