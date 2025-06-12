// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.CSharpProject.Models;

public static class DocumentationConverter
{
    /// <summary>
    /// Converts the xml documentation string into a plain text string.
    /// </summary>
    public static FormattedValue GetDocumentation(ISymbol symbol, string lineEnding)
    {
        string documentation;
        switch (symbol)
        {
            case IParameterSymbol parameter:
                documentation = $"**{parameter.Name}**: {GetParameterDocumentation(parameter, lineEnding)}";
                break;
            case ITypeParameterSymbol typeParam:
                documentation = $"**{typeParam.Name}**: {GetTypeParameterDocumentation(typeParam, lineEnding)}";
                break;
            case IAliasSymbol alias:
                documentation = GetAliasDocumentation(alias, lineEnding);
                break;
            default:
                documentation = GetDocumentationComment(symbol, lineEnding).SummaryText;
                break;
        }

        if (string.IsNullOrEmpty(documentation))
        {
            return null;
        }

        return new("text/markdown", documentation);
    }

    public static DocumentationComment GetDocumentationComment(ISymbol symbol, string lineEnding)
    {
        return DocumentationComment.From(symbol.GetDocumentationCommentXml(), lineEnding);
    }

    private static string GetParameterDocumentation(IParameterSymbol parameter, string lineEnding = "\n")
    {
        var contaningSymbolDef = parameter.ContainingSymbol.OriginalDefinition;
        return GetDocumentationComment(contaningSymbolDef, lineEnding)
            .GetParameterText(parameter.Name);
    }

    private static string GetTypeParameterDocumentation(ITypeParameterSymbol typeParam, string lineEnding = "\n")
    {
        var contaningSymbol = typeParam.ContainingSymbol;
        return GetDocumentationComment(contaningSymbol, lineEnding)
            .GetTypeParameterText(typeParam.Name);
    }

    private static string GetAliasDocumentation(IAliasSymbol alias, string lineEnding = "\n")
    {
        var target = alias.Target;
        return GetDocumentationComment(target, lineEnding).SummaryText;
    }
}