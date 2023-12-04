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
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Parsing;

public class SubmissionParser
{
    private readonly Kernel _kernel;
    private Parser _directiveParser;
    private RootCommand _rootCommand;
    private Dictionary<Type, string> _customInputTypeHints;

    public SubmissionParser(Kernel kernel)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
    }

    public IReadOnlyList<Command> Directives => _rootCommand?.Subcommands ?? Array.Empty<Command>();

    public PolyglotSyntaxTree Parse(string code, string language = null)
    {
        var sourceText = SourceText.From(code);

        var parser = new PolyglotSyntaxParser(
            sourceText,
            language ?? DefaultKernelName(),
            GetDirectiveParser(),
            GetSubkernelDirectiveParsers());

        return parser.Parse();
    }

    public IReadOnlyList<KernelCommand> SplitSubmission(SubmitCode submitCode) =>
        SplitSubmission(
            submitCode,
            submitCode.Code,
            (languageNode, parent, kernelNameNode) =>
            {
                if (!string.IsNullOrWhiteSpace(languageNode.Text))
                {
                    return new SubmitCode(languageNode, kernelNameNode);
                }
                else
                {
                    return null;
                }
            });

    public IReadOnlyList<KernelCommand> SplitSubmission(RequestDiagnostics requestDiagnostics)
    {
        var commands = SplitSubmission(
            requestDiagnostics,
            requestDiagnostics.Code,
            (languageNode, parent, _) => new RequestDiagnostics(languageNode));

        return commands.Where(c => c is RequestDiagnostics).ToList();
    }

    private delegate KernelCommand CreateChildCommand(
        LanguageNode languageNode,
        KernelCommand parentCommand,
        KernelNameDirectiveNode kernelNameDirectiveNode);

    private IReadOnlyList<KernelCommand> SplitSubmission(
        KernelCommand originalCommand,
        string code,
        CreateChildCommand createCommand)
    {
        var commands = new List<KernelCommand>();
        var nugetRestoreOnKernels = new HashSet<string>();
        var hoistedCommandsIndex = 0;

        var tree = Parse(code, originalCommand.TargetKernelName);
        var nodes = tree.GetRoot().ChildNodes.ToArray();

        var targetKernelName = originalCommand.TargetKernelName ?? DefaultKernelName();

        var lastCommandScope = originalCommand.SchedulingScope;
        KernelNameDirectiveNode lastKernelNameNode = null;

        foreach (var node in nodes)
        {
            switch (node)
            {
                case DirectiveNode directiveNode:
                    if (KernelInvocationContext.Current is { } context)
                    {
                        context.CurrentlyParsingDirectiveNode = directiveNode;
                    }

                    var parseResult = directiveNode.GetDirectiveParseResult();

                    if (parseResult.Errors.Any())
                    {
                        bool accept = false;
                        AnonymousKernelCommand sendExtraDiagnostics = null;

                        if (directiveNode is ActionDirectiveNode adn)
                        {
                            if (IsUnknownDirective(adn) && (adn.IsCompilerDirective || AcceptUnknownDirective(adn)))
                            {
                                accept = true;
                            }
                            else
                            {
                                sendExtraDiagnostics = new((_, context) =>
                                {
                                    var diagnostic = new Diagnostic(
                                        adn.GetLinePositionSpan(),
                                        CodeAnalysis.DiagnosticSeverity.Error,
                                        "NI0001", // QUESTION: (SplitSubmission) what code should this be?
                                        "Unrecognized magic command");
                                    var diagnosticsProduced = new DiagnosticsProduced(new[] { diagnostic }, originalCommand);
                                    context.Publish(diagnosticsProduced);
                                    return Task.CompletedTask;
                                });
                            }
                        }

                        if (accept)
                        {
                            var command = createCommand(directiveNode, originalCommand, lastKernelNameNode);
                            command.KernelChooserParseResult = lastKernelNameNode?.GetDirectiveParseResult();
                            commands.Add(command);
                        }
                        else
                        {
                            commands.Clear();

                            if (sendExtraDiagnostics is { })
                            {
                                commands.Add(sendExtraDiagnostics);
                            }

                            commands.Add(
                                new AnonymousKernelCommand((_, context) =>
                                {
                                    var message =
                                        string.Join(Environment.NewLine,
                                            parseResult.Errors
                                                .Select(e => e.ToString()));

                                    context.Fail(originalCommand, message: message);
                                    return Task.CompletedTask;
                                }));
                        }

                        break;
                    }

                    if (directiveNode is KernelNameDirectiveNode kernelNameNode)
                    {
                        targetKernelName = kernelNameNode.Name;
                        lastKernelNameNode = kernelNameNode;
                    }

                    var directiveCommand = new DirectiveCommand(
                        parseResult,
                        directiveNode)
                    {
                        TargetKernelName = targetKernelName,
                        KernelChooserParseResult = lastKernelNameNode?.GetDirectiveParseResult()
                    };

                    if (parseResult.CommandResult.Command.Name == "#r")
                    {
                        var value = parseResult.GetValueForArgument(parseResult.Parser.FindPackageArgument());

                        if (value?.Value is FileInfo)
                        {
                            var hoistedCommand = createCommand(directiveNode, originalCommand, lastKernelNameNode);
                            hoistedCommand.KernelChooserParseResult = lastKernelNameNode?.GetDirectiveParseResult();
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
                    else if (parseResult.CommandResult.Command.Name == "#i")
                    {
                        directiveCommand.SchedulingScope = lastCommandScope;
                        directiveCommand.TargetKernelName = targetKernelName;
                        AddHoistedCommand(directiveCommand);
                        nugetRestoreOnKernels.Add(targetKernelName);
                    }
                    else
                    {
                        commands.Add(directiveCommand);
                        if (directiveNode is KernelNameDirectiveNode)
                        {
                            hoistedCommandsIndex = commands.Count;
                        }
                    }

                    break;

                case LanguageNode languageNode:
                    if (commands.Count > 0 &&
                        commands[^1] is SubmitCode previous)
                    {
                        previous.Code += languageNode.Text;
                    }
                    else
                    {
                        var command = createCommand(languageNode, originalCommand, lastKernelNameNode);

                        if (command is { })
                        {
                            command.KernelChooserParseResult = lastKernelNameNode?.GetDirectiveParseResult();
                            commands.Add(command);
                        }
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(node));
            }
        }

        foreach (var kernelName in nugetRestoreOnKernels)
        {
            var kernel = _kernel.FindKernelByName(kernelName);

            if (kernel?.SubmissionParser.GetDirectiveParser() is { } parser)
            {
                var restore = new DirectiveCommand(
                    parser.Parse("#!nuget-restore"))
                {
                    SchedulingScope = kernel.SchedulingScope,
                    TargetKernelName = kernelName
                };
                AddHoistedCommand(restore);
            }
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

        static bool IsUnknownDirective(ActionDirectiveNode adn) =>
            adn.GetDirectiveParseResult().Errors.All(e => e.SymbolResult?.Symbol is RootCommand);

        bool AcceptUnknownDirective(ActionDirectiveNode node)
        {
            var kernel = _kernel switch
            {
                // The parent kernel is the one where a directive would be defined, and therefore the one that should decide whether to accept this submission. 
                CompositeKernel composite => composite.FindKernelByName(node.ParentKernelName) ?? _kernel,
                _ => _kernel
            };

            return kernel.AcceptsUnknownDirectives;
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

    internal IDictionary<string, (SchedulingScope commandScope, Func<Parser> getParser)> GetSubkernelDirectiveParsers()
    {
        if (!(_kernel is CompositeKernel compositeKernel))
        {
            return null;
        }

        var dict = new Dictionary<string, (SchedulingScope, Func<Parser>)>();

        foreach (var childKernel in compositeKernel.ChildKernels)
        {
            if (childKernel.ChooseKernelDirective is { } chooseKernelDirective)
            {
                foreach (var alias in chooseKernelDirective.Aliases)
                {
                    dict.Add(alias[2..], GetParser());
                }
            }

            (SchedulingScope, Func<Parser>) GetParser() => (childKernel.SchedulingScope, () => childKernel.SubmissionParser.GetDirectiveParser());
        }

        return dict;
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

    internal static (string targetKernelName, string promptOrValueName) SplitKernelDesignatorToken(string tokenToReplace, string kernelNameIfNotSpecified)
    {
        var parts = tokenToReplace.Split(':');

        var (targetKernelName, valueName) =
            parts.Length == 1
                ? (kernelNameIfNotSpecified, parts[0])
                : (parts[0], parts[1]);

        return (targetKernelName, valueName);
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

        if (context is not { CurrentlyParsingDirectiveNode: ActionDirectiveNode { AllowValueSharingByInterpolation: true } })
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

            // FIX: (InterpolateValueFromKernel)    bool persistent = false;

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
                    case "save":
                        // FIX: (InterpolateValueFromKernel) persistent = true;
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
                inputTypeHint: typeHint)
            {
                // Persistent = persistent
            };

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

    internal void ResetParser() => _directiveParser = null;

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