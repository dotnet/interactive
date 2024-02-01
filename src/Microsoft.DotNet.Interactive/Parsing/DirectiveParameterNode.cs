// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class DirectiveParameterNode : SyntaxNode
{
    internal DirectiveParameterNode(SourceText sourceText, SyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public DirectiveParameterNameNode? NameNode { get; private set; }

    public DirectiveParameterValueNode? ValueNode { get; private set; }

    public void Add(DirectiveParameterNameNode node)
    {
        AddInternal(node);
        NameNode = node;
    }

    public void Add(DirectiveParameterValueNode valueNode)
    {
        AddInternal(valueNode);
        ValueNode = valueNode;
    }

    public override IEnumerable<CodeAnalysis.Diagnostic> GetDiagnostics()
    {
        foreach (var diagnostic in base.GetDiagnostics())
        {
            yield return diagnostic;
        }

        if (GetKernelInfo() is { } kernelInfo)
        {
            if (NameNode is { Text: { } parameterName })
            {
                if (Parent is DirectiveNode { DirectiveNameNode.Text: { } directiveName } &&
                    kernelInfo.TryGetDirective(directiveName, out var directive) &&
                    directive is KernelActionDirective actionDirective)
                {
                    if (actionDirective.TryGetParameter(parameterName, out var option))
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
            }
        }
    }
}
