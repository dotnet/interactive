// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class DirectiveNode : TopLevelSyntaxNode
{
    private ParseResult? _parseResult;

    internal DirectiveNode(
        string targetKernelName,
        SourceText sourceText,
        PolyglotSyntaxTree syntaxTree) : base(targetKernelName, sourceText, syntaxTree)
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

            if (TryGetActionDirective(out var actionDirective))
            {
                foreach (var namedParameter in actionDirective.Parameters)
                {
                    if (namedParameter.Required)
                    {
                        var matchingNodes = ChildNodes.OfType<DirectiveParameterNode>()
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

            var foundParameter = false;

            foreach (var childNode in ChildNodes)
            {
                if (childNode is DirectiveSubcommandNode)
                {
                    if (foundParameter)
                    {
                        yield return childNode.CreateDiagnostic(
                            new(PolyglotSyntaxParser.ErrorCodes.ParametersMustAppearAfterSubcommands,
                                "Parameters must appear after subcommands.", DiagnosticSeverity.Error));
                    }
                }
                else if (childNode is DirectiveParameterNode)
                {
                    foundParameter = true;
                }
            }
        }
    }

    private bool TryGetActionDirective([MaybeNullWhen(false)] out KernelActionDirective actionDirective)
    {
        if (GetKernelInfo() is { } kernelInfo)
        {
            if (DirectiveNameNode is { Text: { } name } &&
                kernelInfo.TryGetDirective(name, out var directive) &&
                directive is KernelActionDirective kernelActionDirective)
            {
                actionDirective = kernelActionDirective;

                // drill into subcommands if any
                var commands = DescendantNodesAndTokens()
                               .Where(n => n is DirectiveSubcommandNode)
                               .Select(node => node.Text);

                foreach (var subcommandName in commands)
                {
                    if (kernelActionDirective.TryGetSubcommand(subcommandName, out var subcommand))
                    {
                         actionDirective = subcommand;
                    }
                }

                return true;
            }
        }

        actionDirective = null;
        return false;
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

    public void Add(DirectiveParameterNode node)
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

    public async Task<DirectiveBindingResult<string>> TryGetJsonAsync(DirectiveBindingDelegate bind = null)
    {
        var options = new JsonWriterOptions
        {
            Indented = true
        };

        if (!TryGetActionDirective(out var directive))
        {
            return DirectiveBindingResult<string>.Failure(
                CreateDiagnostic(
                    new(PolyglotSyntaxParser.ErrorCodes.MissingSerializationType,
                        "No serialization type defined for {0}. Please specify a serialization type using {1}.{2}.",
                        DiagnosticSeverity.Error,
                        DirectiveNameNode?.Text ?? ToString(),
                        typeof(KernelActionDirective),
                        nameof(KernelActionDirective.DeserializeAs)
                    )));
        }

        using var stream = new MemoryStream();
        await using var writer = new Utf8JsonWriter(stream, options);

        writer.WriteStartObject();

        writer.WriteString("commandType", directive.DeserializeAs?.Name);

        writer.WritePropertyName("command");

        writer.WriteStartObject();

        writer.WriteString("invokedDirective", GetInvokedCommandPath());

        foreach (var parameterNode in DescendantNodesAndTokens().OfType<DirectiveParameterNode>())
        {
            var name = FromKebabToCamelCase(parameterNode.NameNode?.Text);

            var value = parameterNode.ValueNode?.Text;

            if (value?.StartsWith("\"") is true)
            {
                value = JsonSerializer.Deserialize<string>(value);
            }


            writer.WriteString(name, value);
        }

        writer.WriteString("targetKernelName", TargetKernelName);

        writer.WriteEndObject();

        writer.WriteEndObject();

        await writer.FlushAsync();

        string json = Encoding.UTF8.GetString(stream.ToArray());

        return DirectiveBindingResult<string>.Success(json);
    }

    private string? GetInvokedCommandPath()
    {
        var commands = DescendantNodesAndTokensAndSelf()
                       .Where(n => n is DirectiveSubcommandNode or Parsing.DirectiveNameNode)
                       .Select(node => node.Text);

        return string.Join(" ", commands);
    }

    private static readonly Regex _kebabCaseRegex = new("-[\\w]", RegexOptions.Compiled);

    private static string FromKebabToCamelCase(string value) =>
        _kebabCaseRegex.Replace(
            value.TrimStart('-'),
            m => m.ToString().TrimStart('-').ToUpper());
}