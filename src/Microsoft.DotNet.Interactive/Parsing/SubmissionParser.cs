// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Parsing
{
    public class SubmissionParser
    {
        private readonly Kernel _kernel;
        private Parser _directiveParser;
        private RootCommand _rootCommand;

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
                        return new SubmitCode(languageNode, submitCode.SubmissionType, parent, kernelNameNode);
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
                   (languageNode, parent, _) => new RequestDiagnostics(languageNode, parent));
            
            return commands.Where(c => c is RequestDiagnostics ).ToList();
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
                                    sendExtraDiagnostics = new((c, context) =>
                                    {
                                        var diagnostic = new Interactive.Diagnostic(
                                            adn.GetLinePositionSpan(),
                                            CodeAnalysis.DiagnosticSeverity.Error,
                                            "NI0001", // FIX: (SplitSubmission) what code should this be?
                                            "Unrecognized magic command");
                                        var diagnosticsProduced = new DiagnosticsProduced(new[] { diagnostic }, c);
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

                                if (sendExtraDiagnostics is {})
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
                                    }, parent: originalCommand));
                            }

                            break;
                        }

                        if (directiveNode is KernelNameDirectiveNode kernelNameNode)
                        {
                            targetKernelName = kernelNameNode.KernelName;
                            lastKernelNameNode = kernelNameNode;
                        }

                        var directiveCommand = new DirectiveCommand(
                            parseResult,
                            originalCommand,
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
                    {
                        if (commands.Count > 0 &&
                            commands[commands.Count - 1] is SubmitCode previous)
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
                    }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(node));
                }
            }

            foreach (var kernelName in nugetRestoreOnKernels)
            {
                var kernel = _kernel.FindKernel(kernelName);

                if (kernel?.SubmissionParser.GetDirectiveParser() is { } parser)
                {
                    var restore = new DirectiveCommand(
                        parser.Parse("#!nuget-restore"),
                        originalCommand)
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
                return new []{originalCommand};
            }

            foreach (var command in commands)
            {
                command.Parent = originalCommand;
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
                    CompositeKernel composite => composite.FindKernel(node.ParentKernelName),
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

            var dict = new Dictionary<string, (SchedulingScope , Func<Parser>)>();

            foreach (var childKernel in compositeKernel.ChildKernels)
            {
                if (childKernel.ChooseKernelDirective is { } chooseKernelDirective)
                {
                    foreach (var alias in chooseKernelDirective.Aliases)
                    {
                        dict.Add(alias[2..], GetParser());
                    }
                }

                (SchedulingScope, Func<Parser>) GetParser() => (childKernel.SchedulingScope,() => childKernel.SubmissionParser.GetDirectiveParser());
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

            if (KernelInvocationContext.Current?.Command is not SubmitCode)
            {
                return false;
            }

            var parts = tokenToReplace.Split(':');

            var (targetKernelName, valueName) =
                parts.Length == 1
                    ? (_kernel.Name, parts[0])
                    : (parts[0], parts[1]);

            if (targetKernelName is "input" or "password")
            {
                var inputRequest = new RequestInput($"Please enter a value for field \"{valueName}\".", isPassword: targetKernelName == "password");

                var result = _kernel.RootKernel.SendAsync(inputRequest).GetAwaiter().GetResult();

                var events = result.KernelEvents.ToEnumerable().ToArray();
                var valueProduced = events.OfType<InputProduced>().SingleOrDefault();

                if (valueProduced is { })
                {
                    replacementTokens = new[] { valueProduced.Value };
                }

                return true;
            }
            else
            {
                var result = _kernel.RootKernel.SendAsync(new RequestValue(valueName, targetKernelName)).GetAwaiter().GetResult();

                var events = result.KernelEvents.ToEnumerable().ToArray();
                var valueProduced = events.OfType<ValueProduced>().SingleOrDefault();

                if (valueProduced is { } &&
                    valueProduced.FormattedValue.MimeType == "application/json")
                {
                    var stringValue = valueProduced.FormattedValue.Value;

                    var jsonDoc = JsonDocument.Parse(stringValue);

                    object interpolatedValue = jsonDoc.RootElement.ValueKind switch
                    {
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.String => jsonDoc.Deserialize<string>(),
                        JsonValueKind.Number => jsonDoc.Deserialize<double>(),

                        _ => null
                    };

                    if (interpolatedValue is { })
                    {
                        replacementTokens = new[] { $"{interpolatedValue}" };
                        return true;
                    }
                    else
                    {
                        errorMessage = $"Value @{tokenToReplace} cannot be interpolated into magic command:\n{stringValue}";
                        return false;
                    }
                }
                else
                {
                    errorMessage = events.OfType<CommandFailed>().Last().Message;

                    return false;
                }
            }

            static bool ContainsInvalidCharactersForValueReference(ReadOnlySpan<char> tokenToReplace)
            {
                for (var i = 0; i < tokenToReplace.Length; i++)
                {
                    var c = tokenToReplace[i];

                    if (c == '\\' || c == '/')
                    {
                        return true;
                    }
                }

                return false;
            }
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

        internal void ResetParser()
        {
            _directiveParser = null;
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
}