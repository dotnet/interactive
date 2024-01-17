// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class DirectiveNameNode : SyntaxNode
{
    internal DirectiveNameNode(SourceText sourceText, SyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public override IEnumerable<CodeAnalysis.Diagnostic> GetDiagnostics()
    {
        if (GetKernelScope() is { } targetKernelName)
        {
            if (!SyntaxTree.ParserConfiguration.IsDirectiveInScope(targetKernelName, Text, out _))
            {
                yield return CreateDiagnostic(
                    new(PolyglotSyntaxParser.ErrorCodes.UnknownDirective, 
                    "Unknown magic command '{0}'", 
                    DiagnosticSeverity.Error, 
                    Text));
            }
        }
    }
}