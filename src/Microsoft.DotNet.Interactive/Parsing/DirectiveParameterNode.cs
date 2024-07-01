// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        
        if (NameNode is { Text: { } parameterName } nameNode)
        {
            if (Parent is DirectiveNode directiveNode && 
                directiveNode.TryGetDirective(out var directive))
            {
                if (directive.TryGetParameter(parameterName, out var option))
                {
                    var occurrences = Parent
                                      .DescendantNodesAndTokens()
                                      .OfType<DirectiveParameterNameNode>()
                                      .Where(p => p.Text == parameterName)
                                      .ToArray();

                    if (occurrences.Length > option.MaxOccurrences)
                    {
                        yield return CreateDiagnostic(
                            new(PolyglotSyntaxParser.ErrorCodes.TooManyOccurrencesOfParameter,
                                "A maximum of {0} occurrences are allowed for named parameter '{1}'",
                                DiagnosticSeverity.Error,
                                option.MaxOccurrences,
                                parameterName));
                    }
                }
                else
                {
                    var diagnostic = nameNode.CreateDiagnostic(
                        new(PolyglotSyntaxParser.ErrorCodes.UnknownParameterName,
                            "Unrecognized parameter name '{0}'",
                            DiagnosticSeverity.Error,
                            parameterName ?? ""));
                    yield return diagnostic;
                }
            }
        }
    }

    public bool TryGetParameter([MaybeNullWhen(false)] out KernelDirectiveParameter parameter)
    {
        KernelDirective? directive = null;

        if (Parent is DirectiveNode parentDirectiveNode)
        {
            if (!parentDirectiveNode.TryGetDirective(out directive))
            {
                parameter = null;
                return false;
            }
        }
        else if (Parent is DirectiveSubcommandNode &&
                 Parent.Parent is DirectiveNode grandparentDirectiveNode)
        {
            if (grandparentDirectiveNode.TryGetDirective(out directive))
            {
                if (grandparentDirectiveNode.TryGetSubcommand(directive, out var actionDirective))
                {
                    directive = actionDirective;
                }
            }
        }

        if (directive is not null)
        {
            if (NameNode is not null)
            {
                if (directive.TryGetParameter(NameNode.Text, out parameter))
                {
                    return true;
                }
            }
            else if (directive.Parameters.SingleOrDefault(p => p.AllowImplicitName) is { } implicitlyNamedParameter)
            {
                parameter = implicitlyNamedParameter;
                return true;
            }
        }

        parameter = null!;
        return false;
    }
}