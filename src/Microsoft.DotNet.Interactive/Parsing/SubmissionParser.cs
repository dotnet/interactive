// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
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
    private Parser? _directiveParser;
    private PolyglotParserConfiguration _parserConfiguration;
    private RootCommand _rootCommand;
    private Dictionary<Type, string> _customInputTypeHints;

    public SubmissionParser(Kernel kernel)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
    }

    public IReadOnlyList<Command> Directives => _rootCommand?.Subcommands ?? Array.Empty<Command>();

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
                    return new SubmitCode(languageNode, directiveNode);
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
                                        TargetKernelName = targetKernelName,
                                        Handler = (_, _) => Task.CompletedTask
                                    };
                                }

                                hoistedCommandsIndex = commands.Count;

                                commands.Add(directiveCommand);

                                break;

                            case { Kind: DirectiveNodeKind.CompilerDirective }:

                                var valueNode = directiveNode.DescendantNodesAndTokens().OfType<DirectiveParameterValueNode>().SingleOrDefault();

                                if (valueNode.ChildTokens.Any(t => t is { Kind: TokenKind.Word } and { Text: "nuget" }))
                                {
                                    directiveCommand = new DirectiveCommand(directiveNode)
                                    {
                                        TargetKernelName = targetKernelName
                                    };

                                    if (directiveNode.DirectiveNameNode.Text == "#r")
                                    {
                                        // FIX: (SplitSubmission) extract package name and version details

                                        if (valueNode is FileInfo)
                                        {
                                            var hoistedCommand = createChildCommand(directiveNode, originalCommand, lastKernelNameNode);
                                            AddHoistedCommand(hoistedCommand);
                                        }
                                        else
                                        {
                                            directiveCommand.SchedulingScope = lastCommandScope;
                                            directiveCommand.TargetKernelName = targetKernelName;
                                            AddHoistedCommand(directiveCommand);
                                            nugetRestoreOnKernels.Add(targetKernelName);
                                        }
                                    }
                                    else if (directiveNode.DirectiveNameNode.Text == "#i")
                                    {
                                        directiveCommand.SchedulingScope = lastCommandScope;
                                        directiveCommand.TargetKernelName = targetKernelName;
                                        AddHoistedCommand(directiveCommand);
                                        nugetRestoreOnKernels.Add(targetKernelName);
                                    }
                                }
                                else
                                {
                                    CreateCommandOrAppendToPrevious(directiveNode);
                                }

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
            if (commands.Count == 0)
            {
                return true;
            }

            if (commands.Count == 1)
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
            commands.Clear();

            // FIX: (SplitSubmission) 
            if (diagnostic is null)
            {
                
            }

            commands.Add(
                new AnonymousKernelCommand((_, context) =>
                {
                    var diagnosticsProduced = new DiagnosticsProduced(new[] { Diagnostic.FromCodeAnalysisDiagnostic  (diagnostic) }, originalCommand);
                    context.Publish(diagnosticsProduced);

                    context.Fail(originalCommand, message: diagnostic.ToString());
                    return Task.CompletedTask;
                }));
        }

        async Task<KernelCommand> CreateActionDirectiveCommand(DirectiveNode directiveNode, string targetKernelName)
        {
            if (!directiveNode.TryGetActionDirective(out var directive) || 
                directive.KernelCommandType is null)
            {
                // No command serialization needed.
                return new DirectiveCommand(directiveNode)
                {
                    TargetKernelName = targetKernelName
                };
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

                if (directiveCommand.TargetKernelName is null)
                {
                    // FIX: (SplitSubmission) is this needed or does the initial parse always get it right?
                    directiveCommand.TargetKernelName = targetKernelName;
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
    
    internal Parser GetDirectiveParser()
    {
        if (_directiveParser is null)
        {
            EnsureRootCommandIsInitialized();

            var commandLineBuilder =
                new CommandLineBuilder(_rootCommand)
                    .UseTypoCorrections()
                    .UseHelpBuilder(_ => new DirectiveHelpBuilder(_rootCommand.Name))
                    .UseHelp()
                    .EnableDirectives(false)
                    .UseTokenReplacer(InterpolateValueFromKernel)
                    .AddMiddleware(
                        context =>
                        {
                            context.BindingContext
                                   .AddService(
                                       typeof(KernelInvocationContext),
                                       _ => KernelInvocationContext.Current);
                        });

            _directiveParser = commandLineBuilder.Build();
        }

        return _directiveParser;
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

        if (parametersNodeText?[0] == '{')
        {
            requestInput = JsonSerializer.Deserialize<RequestInput>(parametersNode.Text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        else
        {
            if (parametersNodeText?[0] == '"')
            {
                parametersNodeText = JsonSerializer.Deserialize<string>(parametersNode.Text);
            }

            var valueName = GetValueNameFromNameParameter();

            if (parametersNodeText?.Contains(" ") == true)
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
            if (expressionNode.Parent?.Parent is DirectiveParameterNode parameterValueNode &&
                parameterValueNode.TryGetParameter(out var parameter) &&
                parameter.TypeHint is { } typeHint)
            {
                requestInput.InputTypeHint = typeHint;
            }
        }

        var result = await _kernel.SendAsync(requestInput);

        switch (result.Events[^1])
        {
            case CommandSucceeded:
            {
                string boundValue = null;

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

    private bool InterpolateValueFromKernel(
        string tokenToReplace,
        out IReadOnlyList<string> replacementTokens,
        out string errorMessage)
    {
        errorMessage = null;
        replacementTokens = null;

        if (ContainsInvalidCharactersForValueReference(tokenToReplace.AsSpan()))
        {
            // F# verbatim strings should not be replaced but it's hard to detect them because the quotes are also stripped away by the tokenizer, so we use slashes as a proxy to detect file paths
            return false;
        }

        var context = KernelInvocationContext.Current;

        if (context is { Command: not SubmitCode })
        {
            return false;
        }

        var (targetKernelName, promptOrValueName) = SplitKernelDesignatorToken(tokenToReplace, _kernel.Name);

        if (targetKernelName is "input" or "password")
        {
            ReplaceTokensWithUserInput(out replacementTokens);
            return true;
        }

        if (context is not { CurrentlyParsingDirectiveNode: { Kind: DirectiveNodeKind.Action, AllowValueSharingByInterpolation: true } })
        {
            return false;
        }

        var result = _kernel.RootKernel.SendAsync(new RequestValue(promptOrValueName, mimeType: "application/json", targetKernelName: targetKernelName)).GetAwaiter().GetResult();

        var valueProduced = result.Events.OfType<ValueProduced>().SingleOrDefault();

        if (valueProduced is { })
        {
            string interpolatedValue = null;

            if (valueProduced.Value is { } value)
            {
                interpolatedValue = value.ToString();
            }
            else
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
                            JsonValueKind.Object => stringValue,
                            _ => null
                        };

                        interpolatedValue = jsonValue?.ToString();
                        break;
                    }

                    case "text/plain":
                        interpolatedValue = valueProduced.FormattedValue.Value;
                        break;

                    default:
                        errorMessage = result.Events.OfType<CommandFailed>().LastOrDefault()?.Message ?? $"Unsupported MIME type: {valueProduced.FormattedValue.MimeType}";
                        return false;
                }

            replacementTokens = new[] { $"{interpolatedValue}" };
            return true;
        }
        else
        {
            errorMessage = $"Value '{tokenToReplace}' not found in kernel {targetKernelName}";
            return false;
        }

        void ReplaceTokensWithUserInput(out IReadOnlyList<string> replacementTokens)
        {
            string typeHint = null;
            string valueName = null;
            string prompt = null;

            if (context is not { CurrentlyParsingDirectiveNode: { } currentDirectiveNode })
            {
                replacementTokens = null;
                return;
            }

            if (promptOrValueName.Contains(" "))
            {
                prompt = promptOrValueName;
            }
            else
            {
                valueName = promptOrValueName;
                prompt = $"Please enter a value for field \"{promptOrValueName}\".";
            }

            // use the parser to infer a type hint based on the expected type of the argument at the position of the input token
            var replaceMe = "{2AB89A6C-88D9-4C53-8392-A3A4F902A1CA}";

            var fixedUpText = currentDirectiveNode
                              .Text
                              .Replace("\"", "") // paired quotes are removed from tokenToReplace by the command line string splitter so we also need to remove them from the raw text to find possible matches
                              .Replace($"@{tokenToReplace}", replaceMe)
                              .Replace(" @", " ");

            var parseResult = currentDirectiveNode.DirectiveParser.Parse(fixedUpText);

            if (targetKernelName == "password")
            {
                typeHint = "password";
            }
            else if (parseResult.CommandResult.Children.FirstOrDefault(c => c.Tokens.Any(t => t.Value == replaceMe)) is { Symbol: { } symbol })
            {
                typeHint = GetTypeHint(symbol);
            }

            switch (parseResult.CommandResult.Command.Name)
            {
                case "#!set":
                    if (parseResult.CommandResult.Children.OfType<OptionResult>().SingleOrDefault(o => o.Option.Name == "name") is { } nameOptionResult)
                    {
                        valueName = nameOptionResult.GetValueOrDefault<string>();
                    }

                    break;
            }

            var startOfReplaceMe = fixedUpText.IndexOf(replaceMe, StringComparison.OrdinalIgnoreCase);
            var rawTokenTextPlusRawTrailingText = currentDirectiveNode.Text[startOfReplaceMe..];
            var endOfReplaceMe = startOfReplaceMe + replaceMe.Length;
            var rawTrailingText = currentDirectiveNode.Text[Math.Min(endOfReplaceMe, currentDirectiveNode.Text.Length)..];
            string rawTokenText;

            if (rawTrailingText.Length > 0)
            {
                if (rawTokenTextPlusRawTrailingText.EndsWith(rawTrailingText))
                {
                    rawTokenText = rawTokenTextPlusRawTrailingText.Remove(rawTokenTextPlusRawTrailingText.LastIndexOf(rawTrailingText));
                }
                else
                {
                    rawTokenText = rawTokenTextPlusRawTrailingText;
                }
            }
            else
            {
                rawTokenText = rawTokenTextPlusRawTrailingText;
            }

            foreach (var annotation in ParseInputProperties(rawTokenText))
            {
                switch (annotation.key)
                {
                    case "prompt":
                        prompt = annotation.value;
                        break;
                    case "valueName":
                        valueName ??= annotation.value;
                        break;
                    case "type":
                        typeHint = annotation.value;
                        break;
                    default:
                        break;
                }
            }

            var inputRequest = new RequestInput(
                valueName: valueName,
                prompt: prompt,
                inputTypeHint: typeHint);

            var requestInputResult = _kernel.RootKernel.SendAsync(inputRequest).GetAwaiter().GetResult();

            if (requestInputResult.Events.OfType<InputProduced>().SingleOrDefault() is { } valueProduced)
            {
                replacementTokens = new[] { valueProduced.Value };
            }
            else
            {
                replacementTokens = null;
            }
        }

        static bool ContainsInvalidCharactersForValueReference(ReadOnlySpan<char> tokenToReplace)
        {
            for (var i = 0; i < tokenToReplace.Length; i++)
            {
                var c = tokenToReplace[i];

                if (c is '\\' or '/')
                {
                    return true;
                }
            }

            return false;
        }
    }

    private static IEnumerable<(string key, string value)> ParseInputProperties(string input)
    {
        int? quoteStartIndex = null;
        int currentAnnotationStartIndex = 0;

        if (input.StartsWith("@input:"))
        {
            input = input["@input:".Length ..];
        }
        else if (input.StartsWith("@password:"))
        {
            input = input["@password:".Length ..];
        }

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];

            if (c == '"')
            {
                if (quoteStartIndex is { } start)
                {
                    var value = input[(start + 1) .. i];
                    yield return GetPromptOrFieldName(value);
                }
                else
                {
                    quoteStartIndex = i;
                }

                currentAnnotationStartIndex = i + 1;
            }
            else if (c == ',' || i == input.Length - 1)
            {
                var annotation = input[currentAnnotationStartIndex..i];

                if (quoteStartIndex is null)
                {
                    yield return GetPromptOrFieldName(annotation);
                }
                else
                {
                    var keyAndValue = annotation.Split('=');

                    if (annotation.Length > 0)
                    {
                        if (keyAndValue.Length == 1)
                        {
                            yield return (annotation, null);
                        }
                        else
                        {
                            yield return (keyAndValue[0], keyAndValue[1]);
                        }
                    }
                }

                currentAnnotationStartIndex = i + 1;
            }
        }

        static (string key, string value) GetPromptOrFieldName(string value)
        {
            if (value.Contains(" "))
            {
                return ("prompt", value);
            }
            else
            {
                return ("valueName", value);
            }
        }
    }

    private string GetTypeHint(Symbol symbol)
    {
        string hint;

        if (_customInputTypeHints is not null &&
            symbol is IValueDescriptor descriptor &&
            _customInputTypeHints.TryGetValue(descriptor.ValueType, out hint))
        {
            return hint;
        }

        hint = symbol switch
        {
            IValueDescriptor<DateTime> => "datetime-local",
            IValueDescriptor<int> => "number",
            IValueDescriptor<float> => "number",
            IValueDescriptor<FileSystemInfo> => "file",
            IValueDescriptor<Uri> => "url",
            _ => null
        };
        return hint;
    }

    public void AddDirective(Command command)
    {
        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        foreach (var name in new[] { command.Name }.Concat(command.Aliases))
        {
            if (!name.StartsWith("#"))
            {
                throw new ArgumentException($"Invalid directive name \"{name}\". Directives must begin with \"#\".");
            }
        }

        EnsureRootCommandIsInitialized();

        var existingAliases = _rootCommand
                              .Children
                              .OfType<Command>()
                              .SelectMany(c => c.Aliases)
                              .ToArray();

        foreach (var alias in command.Aliases)
        {
            if (existingAliases.Contains(alias))
            {
                throw new ArgumentException($"Alias '{alias}' is already in use.");
            }
        }

        _rootCommand.Add(command);

        ResetParser();
    }

    /// <summary>
    /// Specifies the type hint to be used for a given destination type parsed as a magic command input type. 
    /// </summary>
    /// <remarks>Type hints are loosely based on the types used for HTML <c>input</c> elements. They allow what is ultimately a text input to be presented in a more specific way (e.g. a date or file picker) to a user according to the capabilities of a UI.</remarks>
    public void SetInputTypeHint(Type expectedType, string inputTypeHint)
    {
        if (_customInputTypeHints is null)
        {
            _customInputTypeHints = new();
        }

        _customInputTypeHints[expectedType] = inputTypeHint;
    }

    internal void ResetParser()
    {
        _directiveParser = null;
        _parserConfiguration = null;
    }

    public static CompletionItem CompletionItemFor(string name, ParseResult parseResult)
    {
        var symbol = parseResult.CommandResult
                                .Command
                                .Children
                                .GetByAlias(name);

        var kind = symbol switch
        {
            Option _ => "Property",
            Command _ => "Method",
            _ => "Value"
        };

        var helpBuilder = new DirectiveHelpBuilder(
            parseResult.Parser.Configuration.RootCommand.Name);

        return new CompletionItem(
            displayText: name,
            kind: kind,
            filterText: name,
            sortText: name,
            insertText: name,
            documentation:
            symbol is not null
                ? helpBuilder.GetHelpForSymbol(symbol)
                : null);
    }

    private void EnsureRootCommandIsInitialized()
    {
        _rootCommand ??= new RootCommand();
    }
}