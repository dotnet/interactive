// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal abstract class HttpSyntaxNodeOrToken
{
    private protected HttpSyntaxNodeOrToken(SourceText sourceText, HttpSyntaxTree? syntaxTree)
    {
        SourceText = sourceText;
        SyntaxTree = syntaxTree;
    }

    protected SourceText SourceText { get; }

    public HttpSyntaxNode? Parent { get; internal set; }

    public abstract TextSpan Span { get; }

    public HttpSyntaxTree? SyntaxTree { get; }

    protected List<Diagnostic>? _diagnostics = null;

    /// <summary>
    /// Gets the significant text of the current node or token, without trivia.
    /// </summary>
    public string Text => TextWithTrivia.Trim();

    /// <summary>
    /// Gets the text of the current node or token, including trivia.
    /// </summary>
    public string TextWithTrivia => SourceText.ToString(Span); 

    public override string ToString() => $"{GetType().Name}: {Text}";

    public abstract IEnumerable<Diagnostic> GetDiagnostics();

    public void AddDiagnostic(Diagnostic d)
    {
        _diagnostics ??= new List<Diagnostic>();        
        _diagnostics.Add(d);
        
    }

}
