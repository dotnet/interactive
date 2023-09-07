// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

public abstract class DirectiveNode : LanguageNode
{
    private ParseResult? _parseResult;

    internal DirectiveNode(
        DirectiveToken directiveToken,
        SourceText sourceText,
        PolyglotSyntaxTree? syntaxTree) : base(directiveToken.DirectiveName, sourceText, syntaxTree)
    {
        Add(directiveToken);
    }

    internal Parser? DirectiveParser { get; set; }

    internal bool AllowValueSharingByInterpolation { get; set; }

    public ParseResult GetDirectiveParseResult()
    {
        if (DirectiveParser is null)
        {
            throw new InvalidOperationException($"{nameof(DirectiveParser)} was not set.");
        }

        return _parseResult ??= DirectiveParser.Parse(Text);
    }

    public override IEnumerable<Diagnostic> GetDiagnostics()
    {
        var parseResult = GetDirectiveParseResult();

        foreach (var error in parseResult.Errors)
        {
            yield return new Diagnostic(
                message: error.Message,
                severity: CodeAnalysis.DiagnosticSeverity.Error,
                linePositionSpan: GetLinePositionSpan(), code: "DNI0001");
        }
    }

    internal bool IsCompilerDirective
    {
        get
        {
            var sourceText = SourceText.GetSubText(Span);

            return sourceText[1] != '!';
        }
    }

    internal int GetLine()
    {
        var line = 0;

        for (var i = 0; i < Span.Start; i++)
        {
            if (SourceText[i] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    internal LinePositionSpan GetLinePositionSpan()
    {
        var line = GetLine();

        return new LinePositionSpan(
            new LinePosition(line, Span.Start),
            new LinePosition(line, Span.End));
    }
}