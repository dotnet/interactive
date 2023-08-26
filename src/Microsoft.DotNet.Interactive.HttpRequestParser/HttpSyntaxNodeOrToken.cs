// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal abstract class HttpSyntaxNodeOrToken
{
    protected List<Diagnostic>? _diagnostics = null;

    private protected HttpSyntaxNodeOrToken(SourceText sourceText, HttpSyntaxTree? syntaxTree)
    {
        SourceText = sourceText;
        SyntaxTree = syntaxTree;
    }

    public SourceText SourceText { get; }

    public HttpSyntaxNode? Parent { get; internal set; }

    public abstract bool IsSignificant { get; }

    public abstract TextSpan FullSpan { get; }

    public abstract TextSpan Span { get; }

    public HttpSyntaxTree? SyntaxTree { get; }

    /// <summary>
    /// Gets the significant text of the current node or token, without trivia.
    /// </summary>
    public string Text => SourceText.ToString(Span);

    public override string ToString() => $"{GetType().Name}: {Text}";

    public virtual IEnumerable<Diagnostic> GetDiagnostics()
    {
        if (_diagnostics is not null)
        {
            foreach (var diagnostic in _diagnostics)
            {
                yield return diagnostic;
            }
        }
    }

    public void AddDiagnostic(Diagnostic d)
    {
        _diagnostics ??= new List<Diagnostic>();
        _diagnostics.Add(d);
    }

    public Diagnostic CreateDiagnostic(string message, DiagnosticSeverity severity = DiagnosticSeverity.Error)
    {
        var lines = SourceText.Lines;

        var tokenSpan = lines.GetLinePositionSpan(Span);

        var diagnostic = new Diagnostic(
            LinePositionSpan.FromCodeAnalysisLinePositionSpan(tokenSpan),
            severity,
            "",
            message);

        return diagnostic;
    }
}