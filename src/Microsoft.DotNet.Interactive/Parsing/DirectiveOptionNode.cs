// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class DirectiveSubcommandNode : SyntaxNode
{
    // FIX: (DirectiveSubcommandNode) separate file

    internal DirectiveSubcommandNode(SourceText sourceText, SyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }
}

internal class DirectiveOptionNode : SyntaxNode
{
    internal DirectiveOptionNode(SourceText sourceText, SyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public DirectiveOptionNameNode? OptionNameNode { get; private set; }

    public DirectiveArgumentNode? ArgumentNode { get; private set; }

    public void Add(DirectiveOptionNameNode node)
    {
        AddInternal(node);
        OptionNameNode = node;
    }

    public void Add(DirectiveArgumentNode node)
    {
        AddInternal(node);
        ArgumentNode = node;
    }

    public override IEnumerable<CodeAnalysis.Diagnostic> GetDiagnostics()
    {
        if (GetKernelScope() is { } targetKernelName)
        {
            if (SyntaxTree.ParserConfiguration.KernelInfos.TryGetValue(targetKernelName, out var kernelInfo))
            {
                if (kernelInfo.TryGetDirective(OptionNameNode.Text, out var directive))
                {
                    // FIX: (GetDiagnostics) 
                }
                else
                {
                    yield return CreateDiagnostic(
                        new(PolyglotSyntaxParser.ErrorCodes.UnknownDirective,
                            "Unknown option '{0}'",
                            DiagnosticSeverity.Error,
                            Text));
                }
            }
        }
    }
}