// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class DirectiveParameterNode : SyntaxNode
{
    internal DirectiveParameterNode(
        SourceText sourceText,
        SyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public override IEnumerable<CodeAnalysis.Diagnostic> GetDiagnostics()
    {
        // FIX: (GetDiagnostics) 
        yield break;

        // if (GetKernelInfo() is { } kernelInfo)
        // {
        //     if (Parent is DirectiveNode { DirectiveNameNode.Text: { } directiveName } &&
        //         kernelInfo.TryGetDirective(directiveName, out var directive) &&
        //         directive is KernelActionDirective actionDirective)
        //     {
        //         var occurrences = SyntaxTree.RootNode
        //                                     .DescendantNodesAndTokensAndSelf()
        //                                     .OfType<DirectiveParameterNode>()
        //                                     .ToArray();
        //
        //         if (occurrences.Length > parameter.MaxOccurrences)
        //         {
        //             yield return CreateDiagnostic(
        //                 new(PolyglotSyntaxParser.ErrorCodes.TooManyOccurrencesOfNamedParameter,
        //                     "A maximum of {0} unnamed parameters are allowed",
        //                     DiagnosticSeverity.Error,
        //                     parameter.MaxOccurrences));
        //         }
        //     }
        // }
    }
}