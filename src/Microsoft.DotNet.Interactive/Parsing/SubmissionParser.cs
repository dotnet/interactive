// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
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
            KernelLanguage = kernel switch
            {
                CompositeKernel c => c.DefaultKernelName,
                _ => kernel.Name
            };
        }

        public IReadOnlyList<Command> Directives => _rootCommand?.Children.OfType<Command>().ToArray() ?? Array.Empty<Command>();

        public string KernelLanguage { get; internal set; }

        public PolyglotSyntaxTree Parse(string code, string language = null)
        {
            var sourceText = SourceText.From(code);

            var parser = new PolyglotSyntaxParser(
                sourceText,
                language ?? KernelLanguage,
                GetDirectiveParser(),
                GetSubkernelDirectiveParsers());

            return parser.Parse();
        }

        public IReadOnlyList<KernelCommand> SplitSubmission(SubmitCode submitCode) =>
            SplitSubmission(
                submitCode,
                submitCode.Code,
                (languageNode, parent, kernelNameNode) => new SubmitCode(languageNode, submitCode.SubmissionType, parent, kernelNameNode));

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
            var targetKernelName = originalCommand.TargetKernelName ?? KernelLanguage;
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
                            if (directiveNode.IsUnknownActionDirective())
                            {
                                commands.Add(createCommand(directiveNode, originalCommand, lastKernelNameNode));
                            }
                            else
                            {
                                commands.Clear();
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
                        var kernelCommand = createCommand(languageNode, originalCommand, lastKernelNameNode);
                        kernelCommand.KernelChooserParseResult = lastKernelNameNode?.GetDirectiveParseResult();
                        commands.Add(kernelCommand);
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

                if (commands.All(c => c.GetType() == 
                                      originalCommand.GetType() 
                                      && (
                                          c.TargetKernelName == originalCommand.TargetKernelName||c.TargetKernelName == commands[0].TargetKernelName)))
                {
                    
                    return true;
                }
                
                return false;
            }
        }

        internal IDictionary<string, (SchedulingScope commandScope, Func<Parser> getParser)> GetSubkernelDirectiveParsers()
        {
            if (!(_kernel is CompositeKernel compositeKernel))
            {
                return null;
            }

            var dict = new Dictionary<string, (SchedulingScope , Func<Parser>)>();

            for (var i = 0; i < compositeKernel.ChildKernels.Count; i++)
            {
                var childKernel = compositeKernel.ChildKernels[i];

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
                        .ParseResponseFileAs(ResponseFileHandling.Disabled)
                        .UseTypoCorrections()
                        .UseHelpBuilder(_ => new DirectiveHelpBuilder(_rootCommand.Name))
                        .UseHelp()
                        .AddMiddleware(
                            context =>
                            {
                                context.BindingContext
                                       .AddService(
                                           typeof(KernelInvocationContext),
                                           _ => KernelInvocationContext.Current);
                            });

                commandLineBuilder.EnableDirectives = false;

                _directiveParser = commandLineBuilder.Build();
            }

            return _directiveParser;
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