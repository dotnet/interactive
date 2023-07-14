// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal abstract class HttpSyntaxNodeOrToken
{
    private protected HttpSyntaxNodeOrToken(string sourceText, HttpSyntaxTree? syntaxTree)
    {
        SourceText = sourceText;
        SyntaxTree = syntaxTree;
    }

    protected string SourceText { get; }

    public HttpSyntaxNode? Parent { get; internal set; }

    public abstract TextSpan Span { get; }

    public HttpSyntaxTree? SyntaxTree { get; }

    /// <summary>
    /// Gets the significant text of the current node or token, without trivia.
    /// </summary>
    public string Text => SourceText.Substring(Span.Start, Span.Length).Trim();

    /// <summary>
    /// Gets the text of the current node or token, including trivia.
    /// </summary>
    public string TextWithTrivia => SourceText.Substring(Span.Start, Span.Length);

    public override string ToString() => $"{GetType().Name}: {Text}";
}
