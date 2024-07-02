// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Parsing.Tests.Utility;

internal static class SyntaxNodeExtensions
{
    static SyntaxNodeExtensions()
    {
        Formatter.RecursionLimit = 20;
        Formatter.ListExpansionLimit = 50;

        Formatter.Register<SyntaxNode>((value, context) =>
        {
            context.Writer.WriteLine(value.Text);

            foreach (var childNode in value.ChildNodes)
            {
                context.Writer.Write(new string(' ', context.Depth * 3) + $"-[{childNode.GetType().Name}] ");

                childNode.FormatTo(context, "text/plain");
            }

            return true;
        });
    }

    public static string Diagram(this SyntaxNode node)
    {
        return node.ToDisplayString("text/plain");
    }
}