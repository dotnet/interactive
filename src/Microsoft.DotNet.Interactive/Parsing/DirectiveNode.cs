// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Parsing;

internal class DirectiveNode : TopLevelSyntaxNode
{
    internal DirectiveNode(
        SourceText sourceText,
        PolyglotSyntaxTree syntaxTree) : base(sourceText, syntaxTree)
    {
    }

    public DirectiveNameNode? NameNode { get; private set; }

    public DirectiveSubcommandNode? SubcommandNode { get; private set; }

    public bool HasParameters { get; private set; }

    public DirectiveNodeKind Kind { get; set; }

    /// <summary>
    /// Gets diagnostics for the current node, including diagnostics for descendant nodes.
    /// </summary>
    public override IEnumerable<CodeAnalysis.Diagnostic> GetDiagnostics()
    {
        foreach (var diagnostic in base.GetDiagnostics())
        {
            yield return diagnostic;
        }

        if (TryGetDirective(out var directive))
        {
            foreach (var namedParameter in directive.Parameters)
            {
                if (namedParameter.Required)
                {
                    var matchingNodes = ChildNodes.OfType<DirectiveParameterNode>()
                                                  .Where(p => p.NameNode is null
                                                                  ? namedParameter.AllowImplicitName
                                                                  : p.NameNode?.Text == namedParameter.Name);

                    if (!matchingNodes.Any())
                    {
                        yield return CreateDiagnostic(
                            new(PolyglotSyntaxParser.ErrorCodes.MissingRequiredParameter,
                                "Missing required parameter '{0}'",
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

    public bool TryGetActionDirective(out KernelActionDirective directive)
    {
        if (GetKernelInfo() is { } kernelInfo)
        {
            if (NameNode is { Text: { } name } &&
                kernelInfo.TryGetDirective(name, out var d) &&
                d is KernelActionDirective kernelActionDirective)
            {
                directive = kernelActionDirective;
                return true;
            }
        }

        directive = null!;
        return false;
    }

    public bool TryGetKernelSpecifierDirective(out KernelSpecifierDirective directive)
    {
        if (SyntaxTree.ParserConfiguration.KernelInfos.SingleOrDefault(i => i is { IsComposite: true, IsProxy: false }) is { } compositeKernelInfo)
        {
            if (NameNode is { Text: { } name } &&
                compositeKernelInfo.TryGetDirective(name, out var d) &&
                d is KernelSpecifierDirective kernelSpecifierDirective)
            {
                directive = kernelSpecifierDirective;
                return true;
            }
        }

        directive = null!;
        return false;
    }

    public bool TryGetDirective(out KernelDirective directive)
    {
        if (TryGetActionDirective(out var actionDirective))
        {
            directive = actionDirective;
            return true;
        }

        if (TryGetKernelSpecifierDirective(out var specifierDirective))
        {
            directive = specifierDirective;
            return true;
        }

        directive = null!;
        return false;
    }

    public void Add(DirectiveNameNode node)
    {
        NameNode = node;
        AddInternal(node);
    }

    public void Add(DirectiveSubcommandNode node)
    {
        SubcommandNode = node;
        AddInternal(node);
    }

    public void Add(DirectiveParameterNode node)
    {
        AddInternal(node);
        HasParameters = true;
    }

    public void Add(DirectiveParameterValueNode valueNode)
    {
        AddInternal(valueNode);
        HasParameters = true;
    }

    public IEnumerable<(string Name, object? Value, DirectiveParameterNode? ParameterNode)> GetParameterValues(
        KernelDirective directive,
        Dictionary<DirectiveParameterValueNode, object?> boundExpressionValues)
    {
        var parameterNodes = ChildNodes.OfType<DirectiveParameterNode>().ToArray();

        var parameters = directive.Parameters;

        if (TryGetSubcommand(directive, out var subcommandActionDirective))
        {
            var subcommandParameterNodes = SubcommandNode!.ChildNodes.OfType<DirectiveParameterNode>();
            var subcommandParameters = subcommandActionDirective.Parameters;

            parameterNodes = parameterNodes.Concat(subcommandParameterNodes).ToArray();
            parameters = parameters.Concat(subcommandParameters).ToArray();
        }

        foreach (var valueTuple in BindParameters(parameters, parameterNodes, boundExpressionValues))
        {
            yield return valueTuple;
        }

        static IEnumerable<(string Name, object? Value, DirectiveParameterNode? ParameterNode)> BindParameters(
            ICollection<KernelDirectiveParameter> parameters,
            DirectiveParameterNode[] parameterNodes,
            Dictionary<DirectiveParameterValueNode, object?> boundExpressionValues)
        {
            foreach (var parameter in parameters)
            {
                var matchingNodes = parameterNodes
                                    .Where(node =>
                                               node.NameNode is null
                                                   ? parameter.AllowImplicitName
                                                   : node.NameNode?.Text == parameter.Name)
                                    .ToArray();

                switch (matchingNodes)
                {
                    case [{ } parameterNode]:
                    {
                        if (parameter.Flag)
                        {
                            yield return (parameter.Name, true, parameterNode);
                        }

                        if (parameterNode.ValueNode is not null)
                        {
                            if (boundExpressionValues?.TryGetValue(parameterNode.ValueNode, out var boundValue) is true)
                            {
                                yield return (parameter.Name, boundValue, parameterNode);
                            }
                            else
                            {
                                yield return (parameter.Name, parameterNode.ValueNode.Text, parameterNode);
                            }
                        }

                        break;
                    }

                    case []:
                        if (parameter.Flag)
                        {
                            yield return (parameter.Name, false, null);
                        }

                        break;

                    // FIX: (GetParameters) handle multiple matching nodes for the parameter (write array?)
                }
            }
        }
    }

    public async Task<DirectiveBindingResult<string>> TryGetJsonAsync(DirectiveBindingDelegate? bind = null)
    {
        if (!TryGetActionDirective(out var directive) &&
            directive.KernelCommandType is null)
        {
            return DirectiveBindingResult<string>.Failure(
                CreateDiagnostic(
                    new(PolyglotSyntaxParser.ErrorCodes.MissingSerializationType,
                        "No kernel command type type defined for {0}. Please specify a kernel command type using {1}.{2}.",
                        DiagnosticSeverity.Error,
                        NameNode?.Text ?? ToString(),
                        typeof(KernelActionDirective),
                        nameof(KernelActionDirective.KernelCommandType)
                    )));
        }

        if (GetDiagnostics().FirstOrDefault() is { } diagnostic)
        {
            return DirectiveBindingResult<string>.Failure(diagnostic);
        }

        var (boundExpressionValues, diagnostics) = await TryBindExpressionsAsync(bind);

        if (diagnostics.Length > 0)
        {
            return DirectiveBindingResult<string>.Failure(diagnostics);
        }

        var options = new JsonWriterOptions
        {
            Indented = true
        };

        using var stream = new MemoryStream();
        await using var writer = new Utf8JsonWriter(stream, options);

        writer.WriteStartObject();

        if (TryGetSubcommand(directive, out var subcommandDirective))
        {
            writer.WriteString("commandType", subcommandDirective.KernelCommandType?.Name);
        }
        else
        {
            writer.WriteString("commandType", directive.KernelCommandType?.Name);
        }

        writer.WritePropertyName("command");

        writer.WriteStartObject();

        IEnumerable<(string Name, object? Value, DirectiveParameterNode? ParameterNode)> parameterValues = GetParameterValues(directive, boundExpressionValues).ToArray();

        foreach (var parameter in parameterValues)
        {
            var parameterName = FromPosixStyleToCamelCase(parameter.Name);

            switch (parameter.Value)
            {
                case bool value:
                    writer.WriteBoolean(parameterName, value);
                    break;

                case string stringValue:
                    WriteProperty(parameterName, stringValue);
                    break;

                case null:
                    writer.WriteNull(parameterName);
                    break;

                default:
                    writer.WritePropertyName(parameterName);
                    writer.WriteRawValue(JsonSerializer.Serialize(parameter.Value));
                    break;
            }
        }

        writer.WriteString("invokedDirective", GetInvokedCommandPath());
        writer.WriteString("targetKernelName", TargetKernelName);

        writer.WriteEndObject();

        writer.WriteEndObject();

        await writer.FlushAsync();

        var json = Encoding.UTF8.GetString(stream.ToArray());

        return DirectiveBindingResult<string>.Success(json);

        void WriteProperty(string propertyName, string value)
        {
            if (value[0] is '{' or '"' or '[')
            {
                writer.WritePropertyName(propertyName);
                writer.WriteRawValue(value);
            }
            else
            {
                writer.WriteString(propertyName, value);
            }
        }
    }

    public async Task<(Dictionary<DirectiveParameterValueNode, object?> boundExpressionValues, CodeAnalysis.Diagnostic[] diagnostics)> TryBindExpressionsAsync(
        DirectiveBindingDelegate? bind)
    {
        Dictionary<DirectiveParameterValueNode, object?> boundExpressionValues = new();

        if (DescendantNodesAndTokens().OfType<DirectiveExpressionTypeNode>() is { } expressionTypeNodes)
        {
            if (bind is null)
            {
                if (expressionTypeNodes.FirstOrDefault() is { } firstExpressionTypeNode)
                {
                    var diagnostic = firstExpressionTypeNode.CreateDiagnostic(
                        new(PolyglotSyntaxParser.ErrorCodes.MissingBindingDelegate,
                            $"When bindings are present then a {nameof(DirectiveBindingDelegate)} must be provided.",
                            DiagnosticSeverity.Error));

                    return (boundExpressionValues, new[] { diagnostic });
                }
            }
            else
            {
                foreach (var expressionNode in DescendantNodesAndTokens().OfType<DirectiveExpressionNode>())
                {
                    var bindingResult = await bind(expressionNode);

                    if (bindingResult.IsSuccessful)
                    {
                        boundExpressionValues.Add(
                            (DirectiveParameterValueNode)expressionNode.Parent!,
                            bindingResult.Value);
                    }
                    else
                    {
                        var diagnostics = bindingResult.Diagnostics.ToArray();
                        return (boundExpressionValues, diagnostics);
                    }
                }
            }
        }

        return (boundExpressionValues, Array.Empty<CodeAnalysis.Diagnostic>());
    }

    internal bool TryGetSubcommand(
        KernelDirective parentDirective,
        [NotNullWhen(true)] out KernelActionDirective? subcommandDirective)
    {
        if (SubcommandNode is { NameNode: { } subcommandNameNode } &&
            parentDirective is KernelActionDirective selfAsActionDirective &&
            selfAsActionDirective.TryGetSubcommand(subcommandNameNode.Text, out subcommandDirective))
        {
            return true;
        }
        else
        {
            subcommandDirective = null;
            return false;
        }
    }

    public string GetInvokedCommandPath()
    {
        if (SubcommandNode is { NameNode: { } subcommandNameNode })
        {
            return $"{NameNode?.Text} {subcommandNameNode.Text}";
        }
        else
        {
            return $"{NameNode?.Text}";
        }
    }

    private static readonly Regex _kebabCaseRegex = new("-[\\w]", RegexOptions.Compiled);

    private static string FromPosixStyleToCamelCase(string value) =>
        _kebabCaseRegex.Replace(
            value.TrimStart('-'),
            m => m.ToString().TrimStart('-').ToUpper());

    public async Task<IReadOnlyList<CompletionItem>> GetCompletionsAtPositionAsync(int position)
    {
        var node = FindNode(position);
        var currentToken = FindToken(position);

        // FIX: (GetCompletionsAtPositionAsync) delete this
        switch (TargetKernelName)
        {
            case ".NET":
                break;
            case "csharp":
                break;
        }

        switch (node)
        {
            case DirectiveNameNode:
                if (TryGetDirective(out var directive))
                {
                    var completions = await directive.GetChildCompletionsAsync();

                    if (directive is KernelActionDirective actionDirective &&
                        actionDirective.Subcommands.Any() &&
                        !DescendantNodesAndTokens().OfType<DirectiveSubcommandNode>().Any(n => n.Span.End <= position))
                    {
                        completions = completions.Where(c => c.AssociatedSymbol is KernelDirective).ToList();
                    }

                    return completions;
                }
                else
                {
                    // FIX: (GetCompletionsAtPositionAsync) handle the case where the directive is not found

                    if (node?.Text.StartsWith("#!") is true)
                    {
                        return GetCompletionsForPartialDirective();
                    }
                  
                  



                }

                break;

            case DirectiveParameterNameNode directiveParameterNameNode:
            {
                if (directiveParameterNameNode?.Parent is DirectiveParameterNode pn &&
                    pn.TryGetParameter(out var parameter) &&
                    currentToken is { Kind: TokenKind.Whitespace })
                {
                    var completions = await parameter.GetValueCompletionsAsync();
                    return completions;
                }
            }

                break;

            case DirectiveSubcommandNode subcommandNode:
            {
                if (subcommandNode.TryGetSubcommand(out var subcommandDirective) &&
                    TryGetDirective(out var parentDirective))
                {
                    // parent directive parameter completions are valid under subcommands
                    var completions = (await subcommandDirective.GetChildCompletionsAsync()).ToList();

                    var parentDirectiveParameterCompletions = await parentDirective.GetChildCompletionsAsync();

                    foreach (var completionItem in parentDirectiveParameterCompletions)
                    {
                        if (completionItem.AssociatedSymbol is not KernelDirective)
                        {
                            completions.Add(completionItem);
                        }
                    }

                    return completions;
                }
            }
                break;

            case DirectiveNode directiveNode:

                return GetCompletionsForPartialDirective();

            case DirectiveParameterValueNode directiveParameterValueNode:
            {
                if (directiveParameterValueNode.Parent is DirectiveParameterNode pn &&
                    pn.TryGetParameter(out var parameter))
                {
                     var completions = await parameter.GetValueCompletionsAsync();
                    return completions;
                }
            }

                break;

            case DirectiveExpressionNode directiveExpressionNode:
                break;

            case DirectiveExpressionParametersNode directiveExpressionParametersNode:
                break;

            case DirectiveExpressionTypeNode directiveExpressionTypeNode:
                break;

            case DirectiveParameterNode parameterNode:

                break;

        

            default:
                break;
        }

        return [];

        CompletionItem[] GetCompletionsForPartialDirective()
        {
            return node.SyntaxTree
                       .ParserConfiguration
                       .KernelInfos
                       .SelectMany(i => i.SupportedDirectives
                                         .Where(d => !d.Hidden)
                                         .Select(d => new CompletionItem(d.Name, WellKnownTags.Method)))
                       .ToArray();
        }
    }
}