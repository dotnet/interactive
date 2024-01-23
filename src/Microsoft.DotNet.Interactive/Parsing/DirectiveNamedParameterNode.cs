// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class DirectiveSubcommandNode : SyntaxNode
{
    // FIX: (DirectiveSubcommandNode) separate file

    internal DirectiveSubcommandNode(SourceText sourceText, SyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }
}

internal class DirectiveNamedParameterNode : SyntaxNode
{
    internal DirectiveNamedParameterNode(SourceText sourceText, SyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public DirectiveParameterNameNode? NameNode { get; private set; }

    public DirectiveParameterNode? ArgumentNode { get; private set; }

    public void Add(DirectiveParameterNameNode node)
    {
        AddInternal(node);
        NameNode = node;
    }

    public void Add(DirectiveParameterNode node)
    {
        AddInternal(node);
        ArgumentNode = node;
    }

    public override IEnumerable<CodeAnalysis.Diagnostic> GetDiagnostics()
    {
        if (GetKernelInfo() is{} kernelInfo)
        {
            if (NameNode is { Text: { } parameterName })
            {
                if (Parent is DirectiveNode { DirectiveNameNode.Text: { } directiveName } &&
                    kernelInfo.TryGetDirective(directiveName, out var directive) &&
                    directive is KernelActionDirective actionDirective)
                {
                    if (actionDirective.TryGetNamedParameter(parameterName, out var option))
                    {
                        var occurrences = SyntaxTree.RootNode
                                                    .DescendantNodesAndTokensAndSelf()
                                                    .OfType<DirectiveParameterNameNode>()
                                                    .Where(p => p.Text == parameterName)
                                                    .ToArray();

                        if (occurrences.Length > option.MaxOccurrences)
                        {
                            yield return CreateDiagnostic(
                                new(PolyglotSyntaxParser.ErrorCodes.TooManyOccurrencesOfNamedParameter,
                                    "A maximum of {0} occurrences are allowed for named parameter '{1}'",
                                    DiagnosticSeverity.Error,
                                    option.MaxOccurrences,
                                    parameterName));
                        }
                    }
                }
                else
                {
                    yield return CreateDiagnostic(
                        new(PolyglotSyntaxParser.ErrorCodes.UnknownDirective,
                            "Unknown named parameter '{0}'",
                            DiagnosticSeverity.Error,
                            Text));
                }
            }
        }
    }
}
