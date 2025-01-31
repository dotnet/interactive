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
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Utility;

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

        var configuration = GetParserConfiguration();

        var parser = new PolyglotSyntaxParser(
            sourceText,
            configuration);

        return parser.Parse(defaultKernelName);
    }

    public DirectiveParseResult ParseDirectiveLine(string directiveLine)
    {
        var tree = Parse(directiveLine);

        DirectiveParseResult parseResult = new();

        if (tree.RootNode.ChildNodes.FirstOrDefault() is DirectiveNode directiveNode)
        {
            parseResult.CommandName = directiveNode.NameNode?.Text;

            var parameterValues = directiveNode.GetParameterValues(new());
            foreach (var (name, value, _) in parameterValues)
            {
                parseResult.Parameters[name] = value?.ToString();
            }

            foreach (var expressionNode in directiveNode
                                           .DescendantNodesAndTokens()
                                           .OfType<DirectiveExpressionNode>())
            {
                if (expressionNode.IsInputExpression)
                {
                    var requestInput = RequestInput.Parse(expressionNode);

                    var valueName = requestInput.ParameterName;
                    var prompt = requestInput.Prompt;

                    if (parseResult.CommandName is "#!value" or "#!set" or "#!share")
                    {
                        // valueName should be the value passed to the --name parameter
                        var nameParameterValue = directiveNode
                                                 .DescendantNodesAndTokens()
                                                 .OfType<DirectiveParameterNode>()
                                                 .FirstOrDefault(p => p.NameNode?.Text is "--name")
                                                 ?.DescendantNodesAndTokens()
                                                 .OfType<DirectiveParameterValueNode>()
                                                 .SingleOrDefault()
                                                 ?.Text;
                        if (!string.IsNullOrWhiteSpace(nameParameterValue))
                        {
                            valueName = nameParameterValue;
                        }
                    }

                    if (!prompt.Contains(" "))
                    {
                        valueName = prompt;
                    }

                    var inputField = new InputField(
                        valueName: valueName,
                        prompt: prompt,
                        typeHint: requestInput.InputTypeHint);

                    parseResult.InputFields.Add(inputField);
                }
            }
        }

        return parseResult;
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
                                ClearCommandsAndFail(diagnostics);
                                break;

                            case { Kind: DirectiveNodeKind.KernelSelector }:
                                ClearCommandsAndFail(diagnostics);
                                break;
                        }
                    }
                    else
                    {
                        KernelCommand directiveCommand = null;

                        switch (directiveNode)
                        {
                            case { Kind: DirectiveNodeKind.Action }:

                                if (await CreateActionDirectiveCommand(directiveNode) is { } actionDirectiveCommand)
                                {
                                    commands.Add(actionDirectiveCommand);
                                }

                                break;

                            case { Kind: DirectiveNodeKind.CompilerDirective }:

                                var valueNode = directiveNode.DescendantNodesAndTokens()
                                                             .OfType<DirectiveParameterValueNode>()
                                                             .SingleOrDefault();

                                if (valueNode.ChildTokens.FirstOrDefault(t => t is { Kind: TokenKind.Word }) is { Text: "nuget" })
                                {
                                    if (await CreateActionDirectiveCommand(directiveNode) is { } actionDirectiveCmd)
                                    {
                                        directiveCommand = actionDirectiveCmd;
                                        directiveCommand.SchedulingScope = lastCommandScope;
                                        directiveCommand.TargetKernelName = targetKernelName;
                                        AddHoistedCommand(directiveCommand);
                                        nugetRestoreOnKernels.Add(targetKernelName);
                                    }
                                    else if (directiveNode.GetDiagnostics() is { } ds &&
                                             ds.Any())
                                    {
                                        ClearCommandsAndFail(ds.ToArray());
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
                                                           await RequestAllInputsAndKernelValues(directiveNode, originalCommand),
                                                           _kernel);

                                    if (directiveCommand is null)
                                    {
                                        ClearCommandsAndFail(directiveNode.GetDiagnostics().ToArray());
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
            return [originalCommand];
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

        static bool IsCompilerDirective(DirectiveNode node) => 
            node.Kind is DirectiveNodeKind.CompilerDirective;

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
                previous.Code += Environment.NewLine + languageNode.FullText;
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

        void ClearCommandsAndFail(params CodeAnalysis.Diagnostic[] diagnostics)
        {
            if (diagnostics is null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            commands.Clear();

            commands.Add(
                new AnonymousKernelCommand((_, context) =>
                {
                    var diagnosticsProduced = new DiagnosticsProduced(
                        diagnostics.Select(Diagnostic.FromCodeAnalysisDiagnostic).ToArray(),
                        [new FormattedValue(PlainTextFormatter.MimeType, diagnostics.ToString())],
                        originalCommand);

                    context.Publish(diagnosticsProduced);
                    context.Fail(originalCommand, message: string.Join("\n", diagnostics.Select(d => d.ToString())) );

                    return Task.CompletedTask;
                }));
        }

        async Task<KernelCommand> CreateActionDirectiveCommand(DirectiveNode directiveNode)
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

            if (directive.TryGetKernelCommandAsync is not null && // This indicates that JSON serialization/deserialization of the command from the directive syntax is overridden by custom binding.
                await directive.TryGetKernelCommandAsync(
                    directiveNode,
                    await RequestAllInputsAndKernelValues(directiveNode, originalCommand),
                    _kernel) is { } command)
            {
                var diagnostics = directiveNode.GetDiagnostics().ToArray();

                if (diagnostics is { Length: > 0 })
                {
                    ClearCommandsAndFail(diagnostics);
                    return null;
                }
                else
                {
                    return command;
                }
            }

            var formBindingResult = await RequestMultipleInputsIfAppropriate(directiveNode, originalCommand);

            DirectiveBindingResult<string> serializedCommandResult
                = await directiveNode.TryGetJsonAsync(
                      async expressionNode =>
                      {
                          if (formBindingResult is not null)
                          {
                              if (formBindingResult.BoundValues.FirstOrDefault(v => v.Key.ExpressionNode == expressionNode) is var boundValue)
                              {
                                  return DirectiveBindingResult<object>.Success(boundValue.Value);
                              }
                          }

                          var (bindingResult, _, _) = await RequestSingleValueOrInputAsync(
                                                          expressionNode,
                                                          originalCommand,
                                                          targetKernelName);

                          return bindingResult;
                      });

            // Get command JSON and deserialize.
            if (serializedCommandResult.IsSuccessful)
            {
                IKernelCommandEnvelope commandEnvelope = null;
                try
                {
                    commandEnvelope = KernelCommandEnvelope.Deserialize(serializedCommandResult.Value);
                }
                catch (JsonException exception)
                {
                    PolyglotSyntaxParser.AddDiagnosticForJsonException(directiveNode, exception, SourceText.From(code), out var diagnostic);
                    ClearCommandsAndFail(diagnostic);
                    return null;
                }
                catch (Exception exception)
                {
                    var diagnostic = directiveNode.CreateDiagnostic(
                        new(PolyglotSyntaxParser.ErrorCodes.FailedToDeserialize,
                            exception.Message,
                            DiagnosticSeverity.Error));
                    directiveNode.AddDiagnostic(diagnostic);
                    ClearCommandsAndFail(diagnostic);
                    return null;
                }

                var directiveCommand = commandEnvelope.Command;

                if (directiveCommand is KernelDirectiveCommand kernelDirectiveCommand)
                {
                    kernelDirectiveCommand.DirectiveNode = directiveNode;

                    if (_kernel is CompositeKernel compositeKernel)
                    {
                        var errors = kernelDirectiveCommand.GetValidationErrors(compositeKernel).ToArray();

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
                }

                return directiveCommand;
            }

            ClearCommandsAndFail(serializedCommandResult.Diagnostics.ToArray());
            return null;
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

    private PolyglotParserConfiguration GetParserConfiguration()
    {
        if (_parserConfiguration is null)
        {
            _parserConfiguration = new PolyglotParserConfiguration(DefaultKernelName());

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

    private async Task<ExpressionBindingResult> RequestMultipleInputsIfAppropriate(
        DirectiveNode directiveNode, 
        KernelCommand sourceCommand)
    {
        if (sourceCommand is not SubmitCode)
        {
            return null;
        }

        if (!_kernel.RootKernel.SupportsCommandType(typeof(RequestInputs)))
        {
            return null;
        }

        ExpressionBindingResult formBindingResult = null;

        var expressionNodes = directiveNode.DescendantNodesAndTokens()
                                           .OfType<DirectiveExpressionNode>()
                                           .ToArray();

        if (expressionNodes.Length > 1)
        {
            formBindingResult = new ExpressionBindingResult();

            var requestInputs = new RequestInputs
            {
                Inputs = expressionNodes.Where(n => n.TypeNode?.Type is "input" or "password")
                                        .Select(InputDescription.Parse)
                                        .ToList()
            };

            requestInputs.SetParent(sourceCommand);

            var result = await _kernel.SendAsync(requestInputs);

            if (result.Events.OfType<InputsProduced>().SingleOrDefault() is { } inputsProduced)
            {
                foreach (var input in requestInputs.Inputs)
                {
                    var inputName = input.GetPropertyNameForJsonSerialization();
                    if (inputsProduced.Values.TryGetValue(inputName, out var value))
                    {
                        var directiveParameterValueNode = (DirectiveParameterValueNode)input.ExpressionNode.Parent!;
                        formBindingResult.BoundValues.Add(directiveParameterValueNode, value);
                    }
                }
            }
        }

        return formBindingResult;
    }

    internal async Task<(DirectiveBindingResult<object> bindingResult, ValueProduced valueProduced, InputProduced inputProduced)> RequestSingleValueOrInputAsync(
        DirectiveExpressionNode expressionNode,
        KernelCommand command,
        string targetKernelName)
    {
        if (expressionNode.ChildNodes.OfType<DirectiveExpressionTypeNode>().SingleOrDefault() is not { } expressionTypeNode)
        {
            throw new ArgumentException("Expression type not found");
        }

        var expressionType = expressionTypeNode.Type;

        if (expressionType is "input" or "password")
        {
            if (command is SubmitCode)
            {
                var requestInput = RequestInput.Parse(expressionNode);
                var (bindingResult, inputProduced) = await RequestSingleInput(requestInput, expressionNode);
                return (bindingResult, null, inputProduced);
            }
            else
            {
                return default;
            }
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
        RequestInput requestInput,
        DirectiveExpressionNode expressionNode)
    {
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
    }

    private async Task<ExpressionBindingResult> RequestAllInputsAndKernelValues(
        DirectiveNode directiveNode,
        KernelCommand sourceCommand)
    {
        Dictionary<string, InputProduced> inputsProduced = null;
        Dictionary<string, ValueProduced> valuesProduced = null;

        var formBindindResult = await RequestMultipleInputsIfAppropriate(directiveNode, sourceCommand);

        var (boundValues, diagnostics) =
            await directiveNode.TryBindExpressionsAsync(
                async expressionNode =>
                {
                    if (formBindindResult is not null)
                    {
                        if (formBindindResult.BoundValues.FirstOrDefault(v => v.Key.ExpressionNode == expressionNode) is var boundValue)
                        {
                            return DirectiveBindingResult<object>.Success(boundValue.Value);
                        }
                    }

                    var (bindingResult, valueProduced, inputProduced) =
                        await RequestSingleValueOrInputAsync(
                            expressionNode,
                            sourceCommand,
                            directiveNode.TargetKernelName);

                    if (bindingResult?.IsSuccessful == true &&
                        expressionNode.Parent is { Parent: DirectiveParameterNode { NameNode.Text: { } parameterName } })
                    {
                        if (inputProduced is not null)
                        {
                            inputsProduced ??= new();
                            inputsProduced.Add(parameterName, inputProduced);
                        }

                        if (valueProduced is not null)
                        {
                            valuesProduced ??= new();
                            valuesProduced.Add(parameterName, valueProduced);
                        }
                    }
                    else
                    {
                        if (directiveNode.DescendantNodesAndTokens().OfType<DirectiveExpressionNode>().Any(node => node.IsInputExpression) &&
                            KernelInvocationContext.Current is { Command: SubmitCode } context)
                        {
                            context.Fail(sourceCommand, message: "Input not provided.");
                        }
                    }

                    return bindingResult;
                });

        var result = new ExpressionBindingResult
        {
            BoundValues = boundValues,
            Diagnostics = diagnostics
        };

        if (inputsProduced is not null)
        {
            result.InputsProduced.MergeWith(inputsProduced);
        }

        if (valuesProduced is not null)
        {
            result.ValuesProduced.MergeWith(valuesProduced);
        }

        return result;
    }

    internal void ResetParser()
    {
        _parserConfiguration = null;

        if (_kernel.ParentKernel is {} parentKernel)
        {
            parentKernel.SubmissionParser.ResetParser();
        }
    }
}