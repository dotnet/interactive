// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class DirectiveNode : TopLevelSyntaxNode
{
    private ParseResult? _parseResult;

    internal DirectiveNode(
        string targetKernelName,
        SourceText sourceText,
        PolyglotSyntaxTree? syntaxTree) : base(targetKernelName, sourceText, syntaxTree)
    {
    }

    internal bool AllowValueSharingByInterpolation { get; set; }

    internal Parser? DirectiveParser { get; set; }

    public DirectiveNodeKind Kind { get; set; }

    public ParseResult GetDirectiveParseResult()
    {
        if (DirectiveParser is null)
        {
            throw new InvalidOperationException($"{nameof(DirectiveParser)} was not set.");
        }

        return _parseResult ??= DirectiveParser.Parse(Text);
    }

    public override IEnumerable<CodeAnalysis.Diagnostic> GetDiagnostics()
    {
        var parseResult = GetDirectiveParseResult();

        foreach (var error in parseResult.Errors)
        {
            var descriptor = new DiagnosticInfo(
                id: "DNI0001",
                messageFormat: error.Message,
                severity: DiagnosticSeverity.Error);

            yield return CreateDiagnostic(descriptor);
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

    public string DirectiveName => Text.Split(new[] { ' ', '\t' })[0];

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