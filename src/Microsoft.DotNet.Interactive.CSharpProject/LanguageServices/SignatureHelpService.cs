// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Interactive.CSharpProject.Models;
using Microsoft.DotNet.Interactive.CSharpProject.Models.SignatureHelp;

// Adapted from https://github.com/OmniSharp/omnisharp-roslyn/blob/master/src/OmniSharp.Roslyn.CSharp/Services/Signatures/SignatureHelpService.cs

namespace Microsoft.DotNet.Interactive.CSharpProject.LanguageServices;

internal class SignatureHelpService
{
    public static async Task<SignatureHelpResult> GetSignatureHelpAsync(Func<Task<SemanticModel>> getSemanticModel, SyntaxNode node, int position)
    {
        var invocation = await GetInvocation(getSemanticModel, node, position);
        return InternalGetSignatureHelp(invocation);
    }

    private static SignatureHelpResult InternalGetSignatureHelp(InvocationContext invocation)
    {
        var response = new SignatureHelpResult();

        if (invocation is null)
        {
            return response;
        }

        // define active parameter by position
        foreach (var comma in invocation.Separators)
        {
            if (comma.Span.Start > invocation.Position)
            {
                break;
            }

            response.ActiveParameter += 1;
        }

        // process all signatures, define active signature by types
        var signaturesSet = new HashSet<SignatureInformation>();
        var bestScore = int.MinValue;
        SignatureInformation bestScoredItem = null;

        var types = invocation.ArgumentTypes;
        foreach (var methodOverload in GetMethodOverloads(invocation.SemanticModel, invocation.Receiver))
        {
            var signature = BuildSignature(methodOverload);
            signaturesSet.Add(signature);

            var score = InvocationScore(methodOverload, types);
            if (score > bestScore)
            {
                bestScore = score;
                bestScoredItem = signature;
            }
        }

        var signaturesList = signaturesSet.ToList();
        response.Signatures = signaturesList;
        response.ActiveSignature = signaturesList.IndexOf(bestScoredItem);

        return response;
    }

    internal static async Task<InvocationContext> GetInvocation(Document document, int position)
    {
        var tree = await document.GetSyntaxTreeAsync();
        var root = await tree.GetRootAsync();
        var node = root.FindToken(position).Parent;
        return await GetInvocation(() => document.GetSemanticModelAsync(), node, position);
    }

    internal static async Task<InvocationContext> GetInvocation(Func<Task<SemanticModel>> getSemanticModel, SyntaxNode node, int position)
    {
        // Walk up until we find a node that we're interested in.
        while (node != null)
        {
            switch (node)
            {
                case InvocationExpressionSyntax invocation when invocation.ArgumentList.Span.Contains(position):
                {
                    var semanticModel = await getSemanticModel();
                    return new InvocationContext(semanticModel, position, invocation.Expression, invocation.ArgumentList);
                }
                case ObjectCreationExpressionSyntax objectCreation when objectCreation.ArgumentList?.Span.Contains(position) ?? false:
                {
                    var semanticModel = await getSemanticModel();
                    return new InvocationContext(semanticModel, position, objectCreation, objectCreation.ArgumentList);
                }
                case AttributeSyntax attributeSyntax when attributeSyntax.ArgumentList.Span.Contains(position):
                {
                    var semanticModel = await getSemanticModel();
                    return new InvocationContext(semanticModel, position, attributeSyntax, attributeSyntax.ArgumentList);
                }
            }

            node = node.Parent;
        }

        return null;
    }

    private static IEnumerable<IMethodSymbol> GetMethodOverloads(SemanticModel semanticModel, SyntaxNode node)
    {
        ISymbol symbol = null;
        var symbolInfo = semanticModel.GetSymbolInfo(node);
        if (symbolInfo.Symbol != null)
        {
            symbol = symbolInfo.Symbol;
        }
        else if (!symbolInfo.CandidateSymbols.IsEmpty)
        {
            symbol = symbolInfo.CandidateSymbols.First();
        }

        return symbol?.ContainingType == null
            ? Array.Empty<IMethodSymbol>()
            : symbol.ContainingType.GetMembers(symbol.Name).OfType<IMethodSymbol>();
    }

    private static int InvocationScore(IMethodSymbol symbol, IEnumerable<TypeInfo> types)
    {
        var parameters = GetParameters(symbol).ToList();
        var typeInfos = types.ToList();

        if (parameters.Count() < typeInfos.Count)
        {
            return int.MinValue;
        }

        var score = 0;

        foreach (var (invocation, definition) in typeInfos.Zip(parameters, (i, d) => (i, d)))
        {
            if (invocation.ConvertedType == null)
            {
                // 1 point for having a parameter
                score += 1;
            }

            else if (definition.Type != null && (SymbolEqualityComparer.Default.Equals(invocation.ConvertedType, definition.Type)))

            {
                // 2 points for having a parameter and being
                // the same type
                score += 2;
            }
        }

        return score;
    }

    private static SignatureInformation BuildSignature(IMethodSymbol symbol)
    {
        var documentation = DocumentationConverter.GetDocumentation(symbol, "\n");

        if (documentation is null)
        {
            documentation = new("text/markdown", "");
        }

        var label = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        var parameters = GetParameters(symbol)
            .Select(parameter => new ParameterInformation
            (label: parameter.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                documentation: DocumentationConverter.GetDocumentation(parameter, "\n")))
            .ToArray();

        return new(label, documentation, parameters);
    }

    private static IEnumerable<IParameterSymbol> GetParameters(IMethodSymbol methodSymbol)
    {
        return !methodSymbol.IsExtensionMethod ? methodSymbol.Parameters : methodSymbol.Parameters.RemoveAt(0);
    }
}