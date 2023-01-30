// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation;

public class InstrumentationSyntaxRewriter : CSharpSyntaxRewriter
{
    public static readonly string Sentinel = "6a2f74a2-f01d-423d-a40f-726aa7358a81";

    private readonly VariableLocationMap _variableLocations;
    private readonly AugmentationMap _augmentations;
    private readonly IEnumerable<SyntaxNode> _instrumentedNodes;

    public InstrumentationSyntaxRewriter(
        IEnumerable<SyntaxNode> instrumentedNodes,
        VariableLocationMap printOnce,
        AugmentationMap printEveryStep)
    {
        _variableLocations = printOnce ?? throw new ArgumentNullException(nameof(printOnce));
        _augmentations = printEveryStep ?? throw new ArgumentNullException(nameof(printEveryStep));
        _instrumentedNodes = instrumentedNodes ?? throw new ArgumentNullException(nameof(instrumentedNodes));
    }

    public SyntaxTree ApplyToTree(SyntaxTree tree)
    {
        var newRoot = Visit(tree.GetRoot());
        return tree.WithRootAndOptions(newRoot, tree.Options);
    }

    public override SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> list)
    {
        return SyntaxFactory.List(AugmentWithInstrumentationStatements(list));
    }

    private bool IsEntryPoint(SyntaxNode node)
    {
        if (node.Parent is BlockSyntax && node.Parent.Parent is MethodDeclarationSyntax)
        {
            var method = (MethodDeclarationSyntax) node.Parent.Parent;
            return method.Identifier.Text == "Main";
        }

        return false;
    }

    public IEnumerable<TNode> AugmentWithInstrumentationStatements<TNode>(SyntaxList<TNode> list) where TNode : SyntaxNode
    {
        foreach (var node in list)
        {
            if (IsEntryPoint(node) && node == list.First() && _variableLocations.Data.Count > 0)
            {
                yield return (TNode) (SyntaxNode) CreateStatementToPrintVariableLocations();
            }

            if (_instrumentedNodes.Contains(node) && _augmentations.Data.Count > 0)
            {
                yield return (TNode) (SyntaxNode) CreateStatementToPrintAugmentations(node);
            }

            yield return (TNode) Visit(node);
        }
    }

    private StatementSyntax CreateStatementToPrintAugmentations(SyntaxNode node)
    {
        var augmentation = _augmentations.Data[node];
        var variableInfos = MapAugmentationToVariableInfo(augmentation);
        var filePosition = augmentation.CurrentFilePosition;
        var syntaxNode = CreateSyntaxNode(filePosition, variableInfos);
        return syntaxNode;
    }

    private StatementSyntax CreateStatementToPrintVariableLocations()
    {
        var data = _variableLocations.Serialize();
        var uglifiedData = Regex.Replace(data, "\r|\n", "");
        return SyntaxFactory.ParseStatement($"System.Console.WriteLine(\"{Sentinel}{{{uglifiedData}}}{Sentinel}\");")
            .WithTrailingTrivia(SyntaxFactory.Whitespace("\n"));
    }

    public static StatementSyntax CreateSyntaxNode(
        FilePosition currentFilePosition,
        params VariableInfo[] variables)
    {
        var instrumentationemitter = typeof(InstrumentationEmitter).FullName;
        var emitProgramState = nameof(InstrumentationEmitter.EmitProgramState);
        var getProgramState = nameof(InstrumentationEmitter.GetProgramState);

        return SyntaxFactory.ExpressionStatement(
            CreateMethodInvocation(
                instrumentationemitter,
                emitProgramState,
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.Argument(
                            CreateMethodInvocation(
                                instrumentationemitter,
                                getProgramState,
                                ArgumentListGenerator
                                    .GenerateArgumentListForGetProgramState(
                                        currentFilePosition,
                                        variables.Select(v => ((object) v, v.Name)).ToArray())))
                    })
                ))).WithTrailingTrivia(SyntaxFactory.Whitespace("\n"));
    }

    private static InvocationExpressionSyntax CreateMethodInvocation(string container, string methodName, ArgumentListSyntax arguments)
    {
        return
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(container),
                    SyntaxFactory.Token(SyntaxKind.DotToken),
                    SyntaxFactory.IdentifierName(methodName)),
                arguments
            );
    }

    private VariableInfo[] MapAugmentationToVariableInfo(Augmentation augmentation)
    {
        return augmentation.Locals
            .Concat(augmentation.Parameters)
            .Concat(augmentation.Fields)
            .Concat(augmentation.InternalLocals)
            .Select(variable =>
            {
                var syntax = variable.DeclaringSyntaxReferences.First().GetSyntax();
                var location = syntax.Span;
                if (syntax is VariableDeclaratorSyntax vds)
                {
                    location = vds.Identifier.Span;
                }
                else if (syntax is ForEachStatementSyntax fes)
                {
                    location = fes.Identifier.Span;
                }

                return new VariableInfo
                {
                    Name = variable.Name,
                    Value = JToken.FromObject("unavailable"),
                    RangeOfLines = new LineRange
                    {
                        Start = location.Start,
                        End = location.End
                    }
                };
            }).ToArray();
    }
}