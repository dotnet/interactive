// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class DirectiveExpressionTypeNode : SyntaxNode
{
    private string? _type;

    internal DirectiveExpressionTypeNode(SourceText sourceText, SyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public string Type => _type ??= Text.TrimStart('@').TrimEnd(':');
}