// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

internal sealed class SyntaxToken : SyntaxNodeOrToken
{
    internal SyntaxToken(
        HttpTokenKind kind,
        SourceText sourceText,
        TextSpan fullSpan,
        HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
        Kind = kind;
        FullSpan = fullSpan;
    }

    public override TextSpan FullSpan { get; }

    public override bool IsSignificant => this is not { Kind: HttpTokenKind.Whitespace or HttpTokenKind.NewLine };

    public override TextSpan Span => FullSpan;

    public HttpTokenKind Kind { get; set; }

    public override string ToString() => $"{Kind}: {Text}";
}