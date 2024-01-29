// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Linq;
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

    public DirectiveNameNode? DirectiveNameNode { get; private set; }

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
        if (DirectiveParser is not null)
        {
            // FIX: (GetDiagnostics) remove
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
        else
        {
            foreach (var diagnostic in base.GetDiagnostics())
            {
                yield return diagnostic;
            }

            if (GetKernelInfo() is { } kernelInfo &&
                DirectiveNameNode is { Text: { } name } &&
                kernelInfo.TryGetDirective(name, out var directive) &&
                directive is KernelActionDirective actionDirective)
            {
                foreach (var namedParameter in actionDirective.NamedParameters)
                {
                    if (namedParameter.Required)
                    {
                        var matchingNodes = ChildNodes.OfType<DirectiveNamedParameterNode>()
                                                      .Where(p => p.NameNode?.Text == namedParameter.Name);

                        if (!matchingNodes.Any())
                        {
                            yield return CreateDiagnostic(
                                new(PolyglotSyntaxParser.ErrorCodes.MissingRequiredNamedParameter,
                                    "Missing required named parameter '{0}'",
                                    DiagnosticSeverity.Error,
                                    namedParameter.Name));
                        }
                    }
                }
            }
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

    public void Add(DirectiveNameNode node)
    {
        DirectiveNameNode = node;
        AddInternal(node);
    }

    public void Add(DirectiveParameterValueNode valueNode)
    {
        AddInternal(valueNode);
    }

    public void Add(DirectiveNamedParameterNode node)
    {
        AddInternal(node);
    }

    public void Add(DirectiveSubcommandNode node)
    {
        AddInternal(node);
    }

    public DirectiveBindingResult<object?> CreateFailedBindingResult(DiagnosticInfo diagnosticInfo) =>
        DirectiveBindingResult<object?>.Failure(CreateDiagnostic(diagnosticInfo));

    public DirectiveBindingResult<object?> CreateSuccessfulBindingResult(object? value) =>
        DirectiveBindingResult<object?>.Success(value);
}

internal delegate DirectiveBindingResult<object?> DirectiveBindingDelegate(DirectiveNode node);

internal class DirectiveBindingResult<T>
{
    private DirectiveBindingResult()
    {
    }

    public List<CodeAnalysis.Diagnostic> Diagnostics { get; } = new();

    public bool IsSuccessful { get; private set; }

    public T? Value { get; set; }

    public static DirectiveBindingResult<T> Success(T value, params CodeAnalysis.Diagnostic[] diagnostics)
    {
        if (diagnostics is not null &&
            diagnostics.Any(d => d.Severity is DiagnosticSeverity.Error))
        {
            throw new ArgumentException("Errors must not be present when binding is successful.", nameof(diagnostics));
        }

        var result = new DirectiveBindingResult<T>
        {
            IsSuccessful = true,
            Value = value
        };

        if (diagnostics is not null)
        {
            result.Diagnostics.AddRange(diagnostics);
        }

        return result;
    }

    public static DirectiveBindingResult<T> Failure(params CodeAnalysis.Diagnostic[] diagnostics)
    {
        if (diagnostics is null)
        {
            throw new ArgumentNullException(nameof(diagnostics));
        }

        if (diagnostics.Length is 0)
        {
            throw new ArgumentException("Value cannot be an empty collection.", nameof(diagnostics));
        }

        if (!diagnostics.Any(e => e.Severity is DiagnosticSeverity.Error))
        {
            throw new ArgumentException("At least one error must be present when binding is unsuccessful.", nameof(diagnostics));
        }

        var result = new DirectiveBindingResult<T>
        {
            IsSuccessful = false
        };

        result.Diagnostics.AddRange(diagnostics);

        return result;
    }
}