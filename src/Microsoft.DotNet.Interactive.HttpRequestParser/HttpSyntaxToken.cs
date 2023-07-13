// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal sealed class HttpSyntaxToken : HttpSyntaxNodeOrToken
{
    internal HttpSyntaxToken(
        HttpTokenKind kind,
        string sourceText,
        TextSpan span,
        HttpSyntaxTree? syntaxTree) : base(sourceText, syntaxTree)
    {
        Kind = kind;
        Span = span;
    }

    public override TextSpan Span { get; }

    public HttpTokenKind Kind { get; set; }

    public override string ToString() => $"{Kind}: {Text}";
}