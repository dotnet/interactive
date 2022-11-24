// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Recipes;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation;

public static class ArgumentListGenerator
{
    public static ArgumentListSyntax GenerateArgumentListForGetProgramState(FilePosition filePosition, params (object, string)[] argumentList)
    {
        var variableInfoArgument = argumentList.Select(a =>
        {
            var (argument, value) = a;
            return SyntaxFactory.Argument(
                SyntaxFactory.TupleExpression(
                    SyntaxFactory.SeparatedList(new[]
                    {
                        ConvertObjectToArgument(argument),
                        SyntaxFactory.Argument(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(value)))
                    })));
        }).ToList();

        variableInfoArgument.Insert(0, ConvertObjectToArgument(filePosition));

        return SyntaxFactory.ArgumentList(
            SyntaxFactory.SeparatedList(variableInfoArgument));
    }

    private static ArgumentSyntax ConvertObjectToArgument(object argument)
    {
        return SyntaxFactory.Argument(
            SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Literal(argument.ToJson())));
    }
}