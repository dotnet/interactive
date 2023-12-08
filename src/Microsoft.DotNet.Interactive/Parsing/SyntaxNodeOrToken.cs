// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

internal abstract partial class SyntaxNodeOrToken
{
    protected List<CodeAnalysis.Diagnostic>? _diagnostics = null;

    public SourceText SourceText { get; }

    public SyntaxNode? Parent { get; internal set; }

    public abstract bool IsSignificant { get; }

    public abstract TextSpan FullSpan { get; }

    public abstract TextSpan Span { get; }

    /// <summary>
    /// Gets the significant text of the current node or token, without trivia.
    /// </summary>
    public string Text => SourceText.ToString(Span);

    public override string ToString() => $"{GetType().Name}: {Text}";

    public virtual IEnumerable<CodeAnalysis.Diagnostic> GetDiagnostics()
    {
        if (_diagnostics is not null)
        {
            foreach (var diagnostic in _diagnostics)
            {
                yield return diagnostic;
            }
        }
    }

    public void AddDiagnostic(CodeAnalysis.Diagnostic d)
    {
        _diagnostics ??= new List<CodeAnalysis.Diagnostic>();
        _diagnostics.Add(d);
    }

    public Microsoft.CodeAnalysis.Diagnostic CreateDiagnostic(DiagnosticInfo diagnosticInfo, Location? location = null)
    {
        if (location is null)
        {
            var lineSpan = SourceText.Lines.GetLinePositionSpan(Span);
            location = Location.Create(filePath: string.Empty, Span, lineSpan);
        }

        var descriptor =
            new DiagnosticDescriptor(
                diagnosticInfo.Id,
                title: string.Empty,
                diagnosticInfo.MessageFormat,
                category: DiagnosticCategory,
                diagnosticInfo.Severity,
                isEnabledByDefault: true);

        return Microsoft.CodeAnalysis.Diagnostic.Create(descriptor, location, diagnosticInfo.MessageArguments);
    }
}
