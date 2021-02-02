// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.CSharp.SignatureHelp
{
    internal static class SignatureHelpGenerator
    {
        public static async Task<SignatureHelpProduced> GenerateSignatureInformation(Document document, RequestSignatureHelp command)
        {
            var invocation = await GetInvocation(document, command.LinePosition);
            if (invocation is null)
            {
                return null;
            }

            var activeParameter = 0;

            // define active parameter by position
            foreach (var comma in invocation.Separators)
            {
                if (comma.Span.Start > invocation.Position)
                {
                    break;
                }

                activeParameter++;
            }

            // process all signatures, define active signature by types
            var signatures = new List<SignatureInformation>();
            var bestScore = int.MinValue;
            var bestScoreIndex = 0;

            var types = invocation.ArgumentTypes;
            ISymbol throughSymbol = null;
            ISymbol throughType = null;
            var methodGroup = invocation.SemanticModel.GetMemberGroup(invocation.Receiver).OfType<IMethodSymbol>();
            if (invocation.Receiver is MemberAccessExpressionSyntax)
            {
                var throughExpression = ((MemberAccessExpressionSyntax)invocation.Receiver).Expression;
                throughSymbol = invocation.SemanticModel.GetSpeculativeSymbolInfo(invocation.Position, throughExpression, SpeculativeBindingOption.BindAsExpression).Symbol;
                throughType = invocation.SemanticModel.GetSpeculativeTypeInfo(invocation.Position, throughExpression, SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
                var includeInstance = (throughSymbol != null && !(throughSymbol is ITypeSymbol)) ||
                    throughExpression is LiteralExpressionSyntax ||
                    throughExpression is TypeOfExpressionSyntax;
                var includeStatic = (throughSymbol is INamedTypeSymbol) || throughType != null;
                methodGroup = methodGroup.Where(m => (m.IsStatic && includeStatic) || (!m.IsStatic && includeInstance));
            }
            else if (invocation.Receiver is SimpleNameSyntax && invocation.IsInStaticContext)
            {
                methodGroup = methodGroup.Where(m => m.IsStatic || m.MethodKind == MethodKind.LocalFunction);
            }

            foreach (var methodOverload in methodGroup)
            {
                var signature = BuildSignature(methodOverload);
                signatures.Add(signature);

                var score = InvocationScore(methodOverload, types);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestScoreIndex = signatures.Count - 1;
                }
            }

            return new SignatureHelpProduced(
                command,
                signatures,
                bestScoreIndex,
                activeParameter);
        }

        private static async Task<InvocationContext> GetInvocation(Document document, LinePosition linePosition)
        {
            var text = await document.GetTextAsync();
            var position = text.Lines.GetPosition(linePosition) - 1; // backtrack into the actual invocation
            var tree = await document.GetSyntaxTreeAsync();
            var root = await tree.GetRootAsync();
            var node = root.FindToken(position).Parent;

            // Walk up until we find a node that we're interested in.
            while (node != null)
            {
                if (node is InvocationExpressionSyntax invocation && invocation.ArgumentList.Span.Contains(position))
                {
                    var semanticModel = await document.GetSemanticModelAsync();
                    return new InvocationContext(semanticModel, position, invocation.Expression, invocation.ArgumentList, invocation.IsInStaticContext());
                }

                if (node is ObjectCreationExpressionSyntax objectCreation && objectCreation.ArgumentList.Span.Contains(position))
                {
                    var semanticModel = await document.GetSemanticModelAsync();
                    return new InvocationContext(semanticModel, position, objectCreation, objectCreation.ArgumentList, objectCreation.IsInStaticContext());
                }

                if (node is AttributeSyntax attributeSyntax && attributeSyntax.ArgumentList.Span.Contains(position))
                {
                    var semanticModel = await document.GetSemanticModelAsync();
                    return new InvocationContext(semanticModel, position, attributeSyntax, attributeSyntax.ArgumentList, attributeSyntax.IsInStaticContext());
                }

                node = node.Parent;
            }

            return null;
        }

        private static int InvocationScore(IMethodSymbol symbol, IEnumerable<CodeAnalysis.TypeInfo> types)
        {
            var parameters = symbol.Parameters;
            if (parameters.Count() < types.Count())
            {
                return int.MinValue;
            }

            var score = 0;
            var invocationEnum = types.GetEnumerator();
            var definitionEnum = parameters.GetEnumerator();
            while (invocationEnum.MoveNext() && definitionEnum.MoveNext())
            {
                if (invocationEnum.Current.ConvertedType == null)
                {
                    // 1 point for having a parameter
                    score += 1;
                }
                else if (SymbolEqualityComparer.Default.Equals(invocationEnum.Current.ConvertedType, definitionEnum.Current.Type))
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
            var parameters = symbol.Parameters
                .Select(parameter =>
                    new ParameterInformation(
                        label: parameter.Name,
                        documentation: new FormattedValue("text/markdown", DocumentationConverter.ConvertDocumentation(parameter.GetDocumentationCommentXml(expandIncludes: true)))));

            var signatureInformation = new SignatureInformation(
                label: symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                documentation: new FormattedValue("text/markdown", DocumentationConverter.ConvertDocumentation(symbol.GetDocumentationCommentXml(expandIncludes: true))),
                parameters: parameters.ToList());

            return signatureInformation;
        }
    }
}
