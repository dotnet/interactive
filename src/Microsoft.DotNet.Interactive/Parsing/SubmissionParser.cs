// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Parsing;

public class SubmissionParser
{
    private readonly Kernel _kernel;
    private PolyglotParserConfiguration _parserConfiguration;

    public SubmissionParser(Kernel kernel)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
    }

    internal PolyglotSyntaxTree Parse(string code, string defaultKernelName = null)
    {
        var sourceText = SourceText.From(code);

        var configuration = GetParserConfiguration(defaultKernelName ?? DefaultKernelName());

        var parser = new PolyglotSyntaxParser(
            sourceText,
            configuration);

        return parser.Parse();
    }

    public async Task<IReadOnlyList<KernelCommand>> SplitSubmission(SubmitCode submitCode) =>
        await SplitSubmission(
            submitCode,
            submitCode.Code,
            (languageNode, parent, directiveNode) =>
            {
                if (!string.IsNullOrWhiteSpace(languageNode.Text))
                {
                    return new SubmitCode(languageNode, directiveNode)
                    {
                        SchedulingScope = submitCode.SchedulingScope
                    };
                }
                else
                {
                    return null;
                }
            });

    public async Task<IReadOnlyList<KernelCommand>> SplitSubmission(RequestDiagnostics requestDiagnostics)
    {
        var commands = await SplitSubmission(
                           requestDiagnostics,
                           requestDiagnostics.Code,
                           (languageNode, parent, _) => new RequestDiagnostics(languageNode));

        return commands.Where(c => c is RequestDiagnostics).ToList();
    }

    private delegate KernelCommand CreateChildCommand(
        TopLevelSyntaxNode syntaxNode,
        KernelCommand parentCommand,
        DirectiveNode kernelNameDirectiveNode);

    private async Task<IReadOnlyList<KernelCommand>> SplitSubmission(
        KernelCommand originalCommand,
        string code,
        CreateChildCommand createChildCommand)
    {
        var commands = new List<KernelCommand>();
        var nugetRestoreOnKernels = new HashSet<string>();
        var hoistedCommandsIndex = 0;

        var tree = Parse(code, originalCommand.TargetKernelName);
        var nodes = tree.RootNode.ChildNodes.ToArray();

        var targetKernelName = originalCommand.TargetKernelName ?? DefaultKernelName();

        var lastCommandScope = originalCommand.SchedulingScope;
        DirectiveNode lastKernelNameNode = null;

        foreach (var node in nodes)
        {
            switch (node)
            {
                case DirectiveNode directiveNode:
                    if (KernelInvocationContext.Current is { } context)
                    {
                        context.CurrentlyParsingDirectiveNode = directiveNode;
                    }

                    var diagnostics = directiveNode.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

                    if (diagnostics.Length > 0)
                    {
                        switch (directiveNode)
                        {
                            case { Kind: DirectiveNodeKind.Action } adn when IsUnknownDirective(adn) && (IsCompilerDirective(adn) || AcceptUnknownDirective(adn)):
                                var command = createChildCommand(directiveNode, originalCommand, lastKernelNameNode);
                                commands.Add(command);
                                break;

                            case { Kind: DirectiveNodeKind.Action }:
                                ClearCommandsAndFail(diagnostics[0]);
                                break;

                            case { Kind: DirectiveNodeKind.KernelSelector }:
                                ClearCommandsAndFail(diagnostics[0]);
                                break;
                        }
                    }
                    else
                    {
                        KernelCommand directiveCommand = null;

                        switch (directiveNode)
                        {
                            case { Kind: DirectiveNodeKind.Action }:

                                if (await CreateActionDirectiveCommand(directiveNode, targetKernelName) is { } actionDirectiveCommand)
                                {
                                    commands.Add(actionDirectiveCommand);
                                }

                                break;

                            case { Kind: DirectiveNodeKind.CompilerDirective }:

                                var valueNode = directiveNode.DescendantNodesAndTokens()
                                                             .OfType<DirectiveParameterValueNode>()
                                                             .SingleOrDefault();

                                if (valueNode.ChildTokens.Any(t => t is { Kind: TokenKind.Word } and { Text: "nuget" }))
                                {
                                    if (await CreateActionDirectiveCommand(directiveNode, targetKernelName) is { } actionDirectiveCmd)
                                    {
                                        directiveCommand = actionDirectiveCmd;
                                        directiveCommand.SchedulingScope = lastCommandScope;
                                        directiveCommand.TargetKernelName = targetKernelName;
                                        AddHoistedCommand(directiveCommand);
                                        nugetRestoreOnKernels.Add(targetKernelName);
                                    }
                                    else if (directiveNode.GetDiagnostics() is { } ds &&
                                             ds.FirstOrDefault() is { } d)
                                    {
                                        ClearCommandsAndFail(d);
                                    }
                                }
                                else
                                {
                                    CreateCommandOrAppendToPrevious(directiveNode);
                                }

                                break;

                            case { Kind: DirectiveNodeKind.KernelSelector } kernelNameNode:

                                targetKernelName = kernelNameNode.TargetKernelName;
                                lastKernelNameNode = kernelNameNode;

                                if (directiveNode.TryGetKernelSpecifierDirective(out var kernelSpecifierDirective) &&
                                    kernelSpecifierDirective.TryGetKernelCommandAsync is not null)
                                {
                                    directiveCommand = await kernelSpecifierDirective.TryGetKernelCommandAsync(
                                                           directiveNode, 
                                                           await directiveNode.RequestAllInputsAndKernelValues(_kernel),
                                                           _kernel);

                                    if (directiveCommand is null)
                                    {
                                        ClearCommandsAndFail(directiveNode.GetDiagnostics().FirstOrDefault());
                                        break;
                                    }

                                    directiveCommand.TargetKernelName = targetKernelName;
                                }
                                else
                                {
                                    directiveCommand = new DirectiveCommand(directiveNode)
                                    {
                                        Handler = (_, _) => Task.CompletedTask
                                    };
                                }

                                hoistedCommandsIndex = commands.Count;

                                commands.Add(directiveCommand);

                                break;
                        }
                    }

                    break;

                case LanguageNode languageNode:
                    CreateCommandOrAppendToPrevious(languageNode);

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(node));
            }
        }

        foreach (var kernelName in nugetRestoreOnKernels)
        {
            var kernel = _kernel.FindKernelByName(kernelName);

            // FIX: (SplitSubmission) improve this
            var syntaxTree = PolyglotSyntaxParser.Parse("#!nuget-restore", _parserConfiguration);
            var restore = new DirectiveCommand((DirectiveNode)syntaxTree.RootNode.ChildNodes.Single())
            {
                SchedulingScope = kernel.SchedulingScope,
                TargetKernelName = kernelName
            };
            AddHoistedCommand(restore);
        }

        if (NoSplitWasNeeded())
        {
            originalCommand.TargetKernelName ??= targetKernelName;
            return new[] { originalCommand };
        }

        foreach (var command in commands)
        {
            command.SetParent(originalCommand);
        }

        return commands;

        void AddHoistedCommand(KernelCommand command)
        {
            commands.Insert(hoistedCommandsIndex++, command);
        }

        bool NoSplitWasNeeded()
        {
            if (commands.Count is 0)
            {
                return true;
            }

            if (commands.Count is 1)
            {
                if (commands[0] is SubmitCode sc)
                {
                    if (code.Equals(sc.Code, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            if (commands.All(c => c.GetType() == originalCommand.GetType() &&
                                  (c.TargetKernelName == originalCommand.TargetKernelName
                                   || c.TargetKernelName == commands[0].TargetKernelName)))
            {
                return true;
            }

            return false;
        }

        static bool IsUnknownDirective(DirectiveNode node) =>
            node.GetDiagnostics().Any(d => d.Id == PolyglotSyntaxParser.ErrorCodes.UnknownDirective);

        static bool IsCompilerDirective(DirectiveNode node)
        {
            return node.Kind == DirectiveNodeKind.CompilerDirective;
        }

        bool AcceptUnknownDirective(DirectiveNode node)
        {
            var kernel = _kernel switch
            {
                // The parent kernel is the one where a directive would be defined, and therefore the one that should decide whether to accept this submission. 
                CompositeKernel composite => composite.FindKernelByName(node.TargetKernelName) ?? _kernel,
                _ => _kernel
            };

            return kernel.AcceptsUnknownDirectives;
        }

        void CreateCommandOrAppendToPrevious(TopLevelSyntaxNode languageNode)
        {
            if (commands.Count > 0 &&
                commands[^1] is SubmitCode previous)
            {
                previous.Code += languageNode.Text;
            }
            else
            {
                var command = createChildCommand(languageNode, originalCommand, lastKernelNameNode);

                if (command is not null)
                {
                    commands.Add(command);
                }
            }
        }

        void ClearCommandsAndFail(CodeAnalysis.Diagnostic diagnostic)
        {
            if (diagnostic is null)
            {
                throw new ArgumentNullException(nameof(diagnostic));
            }

            commands.Clear();

            commands.Add(
                new AnonymousKernelCommand((_, context) =>
                {
                    var diagnosticsProduced = new DiagnosticsProduced(
                        [Diagnostic.FromCodeAnalysisDiagnostic(diagnostic)],
                        [new FormattedValue(PlainTextFormatter.MimeType, diagnostic.ToString())],
                        originalCommand);

                    context.Publish(diagnosticsProduced);
                    context.Fail(originalCommand, message: diagnostic.ToString());

                    return Task.CompletedTask;
                }));
        }

        async Task<KernelCommand> CreateActionDirectiveCommand(DirectiveNode directiveNode, string targetKernelName)
        {
            if (!directiveNode.TryGetActionDirective(out var directive))
            {
                // No command serialization needed.
                return new DirectiveCommand(directiveNode);
            }

            if (directive.KernelCommandType is null)
            {
                if (!directiveNode.TryGetSubcommand(directive, out var subcommandDirective) ||
                    subcommandDirective.KernelCommandType is null)
                {
                    // No command serialization needed.
                    return new DirectiveCommand(directiveNode)
                    {
                        TargetKernelName = targetKernelName
                    };
                }
            }

            if (directive.TryGetKernelCommandAsync is not null &&
                await directive.TryGetKernelCommandAsync(
                    directiveNode,
                    await directiveNode.RequestAllInputsAndKernelValues(_kernel),
                    _kernel) is { } command)
            {
                var diagnostics = directiveNode.GetDiagnostics().ToArray();

                if (diagnostics is { Length: > 0 })
                {
                    ClearCommandsAndFail(diagnostics[0]);
                    return null;
                }
                else
                {
                    return command;
                }
            }

            // Get command JSON and deserialize.
            var directiveJsonResult = await directiveNode.TryGetJsonAsync(
                                          async expressionNode =>
                                          {
                                              var (boundValue, _, _) = await RequestSingleValueOrInputAsync(expressionNode, targetKernelName);

                                              return boundValue;
                                          });

            if (directiveJsonResult.IsSuccessful)
            {
                var commandEnvelope = KernelCommandEnvelope.Deserialize(directiveJsonResult.Value);

                var directiveCommand = commandEnvelope.Command;

                if (directiveCommand is KernelDirectiveCommand kernelDirectiveCommand)
                {
                    var errors = kernelDirectiveCommand.GetValidationErrors().ToArray();

                    if (errors.Length > 0)
                    {
                        var diagnostic = directiveNode.CreateDiagnostic(
                            new(PolyglotSyntaxParser.ErrorCodes.CustomMagicCommandError,
                                errors[0], DiagnosticSeverity.Error));
                        directiveNode.AddDiagnostic(diagnostic);

                        ClearCommandsAndFail(diagnostic);
                        return null;
                    }
                }

                return directiveCommand;
            }
            else
            {
                ClearCommandsAndFail(directiveJsonResult.Diagnostics[0]);
                return null;
            }
        }
    }

    private string DefaultKernelName()
    {
        var kernelName = _kernel switch
        {
            CompositeKernel c => c.DefaultKernelName,
            _ => _kernel.Name
        };

        return kernelName;
    }

    private PolyglotParserConfiguration GetParserConfiguration(string defaultKernelName = null)
    {
        if (_parserConfiguration is not null &&
            defaultKernelName is not null &&
            defaultKernelName != DefaultKernelName())
        {
            _parserConfiguration = null;
        }

        if (_parserConfiguration is null)
        {
            _parserConfiguration = new PolyglotParserConfiguration(defaultKernelName ?? DefaultKernelName());

            _parserConfiguration.KernelInfos.Add(_kernel.KernelInfo);

            if (_kernel is CompositeKernel compositeKernel)
            {
                foreach (var kernel in compositeKernel)
                {
                    _parserConfiguration.KernelInfos.Add(kernel.KernelInfo);
                }
            }
        }

        return _parserConfiguration;
    }

    internal static (string targetKernelName, string promptOrValueName) SplitKernelDesignatorToken(
        string expressionText, 
        string kernelNameIfNotSpecified)
    {
        // strip off the leading @ and split on :
        var parts = expressionText[1..].Split(':');

        var (targetKernelName, valueName) =
            parts.Length == 1
                ? (kernelNameIfNotSpecified, parts[0])
                : (parts[0], parts[1]);

        return (targetKernelName, valueName);
    }

    internal async Task<(DirectiveBindingResult<object> boundValue, ValueProduced valueProduced, InputProduced inputProduced)> RequestSingleValueOrInputAsync(
        DirectiveExpressionNode expressionNode,
        string targetKernelName)
    {
        if (expressionNode.ChildNodes.OfType<DirectiveExpressionTypeNode>().SingleOrDefault() is not { } expressionTypeNode)
        {
            throw new ArgumentException("Expression type not found");
        }

        var expressionType = expressionTypeNode.Type;

        if (expressionType is "input" or "password")
        {
            var parametersNode = expressionNode.ChildNodes.OfType<DirectiveExpressionParametersNode>().SingleOrDefault();

            var (bindingResult, inputProduced) = await RequestSingleInput(expressionNode, parametersNode, expressionType);
            return (bindingResult, null, inputProduced);
        }
        else
        {
            var (bindingResult, valueProduced) = await RequestSingleValueFromKernel(
                                                     _kernel,
                                                     expressionNode,
                                                     targetKernelName);
            return (bindingResult, valueProduced, null);
        }
    }

    internal static async Task<(DirectiveBindingResult<object> boundValue, ValueProduced valueProduced)> RequestSingleValueFromKernel(
        Kernel destinationKernel,
        DirectiveExpressionNode expressionNode,
        string targetKernelName)
    {
        if (!(expressionNode
             .Ancestors()
             .OfType<DirectiveNode>()
             .FirstOrDefault() is { } directiveNode))
        {
            throw new ArgumentException($"Parameter '{nameof(expressionNode)}' does not have a parent {nameof(DirectiveNode)}");
        }

        var allowByRef = directiveNode
                      .DescendantNodesAndTokens()
                      .OfType<DirectiveParameterNameNode>()
                      .Any(node => node.Text == "--byref");
       
        var (sourceKernelName, sourceValueName) = SplitKernelDesignatorToken(expressionNode.Text, targetKernelName);

        var sourceKernel = destinationKernel.RootKernel.FindKernelByName(sourceKernelName);
        
        if (allowByRef &&
            sourceKernel is not null &&
            sourceKernel.KernelInfo.IsProxy)
        {
            var diagnostic = expressionNode.CreateDiagnostic(
                new(PolyglotSyntaxParser.ErrorCodes.ByRefNotSupportedWithProxyKernels,
                    LocalizationResources.Magics_set_ErrorMessageSharingByReference(),
                    DiagnosticSeverity.Error));
            return (DirectiveBindingResult<object>.Failure(diagnostic), null);
        }

        var mimeType = allowByRef
                           ? PlainTextFormatter.MimeType
                           : directiveNode
                             .DescendantNodesAndTokens()
                             .OfType<DirectiveParameterNode>()
                             .FirstOrDefault(node => node.NameNode?.Text == "--mime-type")?.ValueNode?.Text
                             ??
                             "application/json";

        return await RequestSingleValueFromKernel(
                   destinationKernel, 
                   sourceKernelName, 
                   sourceValueName, 
                   mimeType, 
                   allowByRef,
                   expressionNode);
    }

    internal static async Task<(DirectiveBindingResult<object> boundValue, ValueProduced valueProduced)> RequestSingleValueFromKernel(
        Kernel destinationKernel,
        string sourceKernelName,
        string sourceValueName,
        string mimeType,
        bool allowByRef,
        DirectiveExpressionNode expressionNode)
    {
        var requestValue = new RequestValue(sourceValueName, mimeType: mimeType, targetKernelName: sourceKernelName);

        var result = await destinationKernel.RootKernel.SendAsync(requestValue);

        switch (result.Events[^1])
        {
            case CommandSucceeded:

                if (result.Events.OfType<ValueProduced>().SingleOrDefault() is not { } valueProduced)
                {
                    break;
                }

                object boundValue = null;

                if (valueProduced.Value is { } value)
                {
                    if (allowByRef)
                    {
                        boundValue = value;
                    }
                    else
                    {
                        // Direct interpolation
                        boundValue = value.ToString();
                    }
                }
                else
                    // Deserialize formatted value.
                    switch (valueProduced.FormattedValue.MimeType)
                    {
                        case "application/json":
                        {
                            var stringValue = valueProduced.FormattedValue.Value;

                            var jsonDoc = JsonDocument.Parse(stringValue);

                            object jsonValue = jsonDoc.RootElement.ValueKind switch
                            {
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                JsonValueKind.String => jsonDoc.Deserialize<string>(),
                                JsonValueKind.Number => jsonDoc.Deserialize<double>(),
                                JsonValueKind.Array => jsonDoc,
                                JsonValueKind.Object => jsonDoc,
                                JsonValueKind.Null => null,
                                JsonValueKind.Undefined => null,
                                _ => null
                            };

                            boundValue = jsonValue;
                            break;
                        }

                        case "text/plain":
                            boundValue = valueProduced.FormattedValue.Value;
                            break;

                        default:
                            var errorMessage = result.Events.OfType<CommandFailed>().LastOrDefault()?.Message ??
                                               $"Unsupported MIME type: {valueProduced.FormattedValue.MimeType}";

                            var diagnostic = expressionNode.CreateDiagnostic(
                                new(PolyglotSyntaxParser.ErrorCodes.UnsupportedMimeType,
                                    errorMessage,
                                    DiagnosticSeverity.Error));

                            return (DirectiveBindingResult<object>.Failure(diagnostic), valueProduced);
                    }

                return (DirectiveBindingResult<object>.Success(boundValue), valueProduced);

            case CommandFailed commandFailed:

                return (DirectiveBindingResult<object>.Failure(
                               expressionNode.CreateDiagnostic(
                                   new(PolyglotSyntaxParser.ErrorCodes.ValueNotFoundInKernel,
                                       commandFailed.Message,
                                       DiagnosticSeverity.Error))),
                           valueProduced: null);
        }

        return (DirectiveBindingResult<object>.Failure(
                       expressionNode.CreateDiagnostic(
                           new(PolyglotSyntaxParser.ErrorCodes.ValueNotFoundInKernel,
                               "Value not found",
                               DiagnosticSeverity.Error))),
                   valueProduced: null);
    }

    private async Task<(DirectiveBindingResult<object> boundValue, InputProduced inputProduced)> RequestSingleInput(
        DirectiveExpressionNode expressionNode, 
        DirectiveExpressionParametersNode parametersNode, 
        string expressionType)
    {
        var parametersNodeText = parametersNode?.Text;

        RequestInput requestInput;

        if (parametersNodeText?[0] is '{')
        {
            requestInput = JsonSerializer.Deserialize<RequestInput>(parametersNode.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        else
        {
            if (parametersNodeText?[0] is '"')
            {
                parametersNodeText = JsonSerializer.Deserialize<string>(parametersNode.Text);
            }

            var valueName = GetValueNameFromNameParameter();

            if (parametersNodeText?.Contains(" ") is true)
            {
                requestInput = new(prompt: parametersNodeText,
                                   valueName: valueName);
            }
            else
            {
                requestInput = new(prompt: $"Please enter a value for field \"{parametersNodeText}\".",
                                   valueName: valueName ?? parametersNodeText);
            }
        }

        if (expressionType is "password")
        {
            requestInput.InputTypeHint = "password";
        }
        else if (string.IsNullOrEmpty(requestInput.InputTypeHint))
        {
            if (expressionNode.Parent?.Parent is DirectiveParameterNode parameterValueNode)
            {
                if (parameterValueNode.TryGetParameter(out var parameter) &&
                    parameter.TypeHint is { } typeHint)
                {
                    requestInput.InputTypeHint = typeHint;
                }
            }
        }

        var result = await _kernel.SendAsync(requestInput);

        switch (result.Events[^1])
        {
            case CommandSucceeded:
            {
                if (result.Events.OfType<InputProduced>().SingleOrDefault() is { } inputProduced)
                {
                    return (DirectiveBindingResult<object>.Success(inputProduced.Value),
                               inputProduced);
                }
                else
                {
                    return (DirectiveBindingResult<object>.Failure(
                                   expressionNode.CreateDiagnostic(
                                       new(PolyglotSyntaxParser.ErrorCodes.InputNotProvided,
                                           "Input not provided",
                                           DiagnosticSeverity.Error))),
                               inputProduced: null);
                }
            }

            case CommandFailed commandFailed:
                return (DirectiveBindingResult<object>.Failure(
                               expressionNode.CreateDiagnostic(
                                   new(PolyglotSyntaxParser.ErrorCodes.InputNotProvided,
                                       commandFailed.Message,
                                       DiagnosticSeverity.Error))),
                           inputProduced: null);

            default:
                throw new ArgumentOutOfRangeException();
        }

        string GetValueNameFromNameParameter()
        {
            if (expressionNode.Ancestors().OfType<DirectiveNode>().FirstOrDefault() is { } directiveNode)
            {
                if (directiveNode.ChildNodes.OfType<DirectiveParameterNode>().FirstOrDefault(n => n.NameNode?.Text == "--name") is { } nameParameterNode)
                {
                    return nameParameterNode.ValueNode.Text;
                }
            }

            return null;
        }
    }

    internal void ResetParser()
    {
        _parserConfiguration = null;
    }
}