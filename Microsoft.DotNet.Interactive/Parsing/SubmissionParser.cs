// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Parsing
{
    public class SubmissionParser
    {
        private readonly KernelBase _kernel;
        private Parser _directiveParser;

        private RootCommand _rootCommand;

        public IReadOnlyList<ICommand> Directives => _rootCommand?.Children.OfType<ICommand>().ToArray() ?? Array.Empty<ICommand>();

        public SubmissionParser(KernelBase kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));

            DefaultLanguage = kernel switch
            {
                CompositeKernel c => c.DefaultKernelName,
                _ => kernel.Name
            };
        }

        public string DefaultLanguage { get; internal set; }

        public PolyglotSyntaxTree Parse(string code)
        {
            var sourceText = SourceText.From(code);

            var parser = new PolyglotSyntaxParser(
                sourceText, 
                DefaultLanguage, 
                GetDirectiveParser(),
                GetSubkernelDirectiveParsers());

            return parser.Parse();
        }

        public const bool USE_NEW_SUBMISSION_SPLITTER = true;

        public IReadOnlyList<IKernelCommand> SplitSubmission(SubmitCode submitCode) =>
            USE_NEW_SUBMISSION_SPLITTER
                ? SplitSubmission_New(submitCode)
                : SplitSubmission_Old(submitCode);

        public IReadOnlyList<IKernelCommand> SplitSubmission_New(
            SubmitCode submitCode)
        {
            var commands = new List<IKernelCommand>();

            var hoistedCommandsIndex = 0;

            var tree = Parse(submitCode.Code);
            var nodes = tree.GetRoot().ChildNodes.ToArray();

            foreach (var node in nodes)
            {
                ParseResult parseResult;
                switch (node)
                {
                    case DirectiveNode directiveNode:

                        parseResult = directiveNode.GetDirectiveParseResult();

                        if (parseResult.Errors.Any())
                        {
                            commands.Clear();
                            commands.Add(
                                new AnonymousKernelCommand((kernelCommand, context) =>
                                {
                                    var message =
                                        string.Join(Environment.NewLine,
                                                    parseResult.Errors
                                                               .Select(e => e.ToString()));

                                    context.Fail(message: message);
                                    return Task.CompletedTask;
                                }));
                            break;
                        }

                        var directiveCommand = new DirectiveCommand(parseResult, directiveNode);
                        
                        if (parseResult.CommandResult.Command.Name == "#r")
                        {
                            var value = parseResult.CommandResult.GetArgumentValueOrDefault<PackageReferenceOrFileInfo>("package");

                            if (value.Value is FileInfo)
                            {
                                // FIX: (SplitSubmission) 
                                AddHoistedCommand(new SubmitCode(directiveNode.Text));
                                // linesToForward.Add(currentLine);
                            }
                            else
                            {
                                AddHoistedCommand(directiveCommand);
                            }
                        }
                        else if (parseResult.CommandResult.Command.Name == "#i")
                        {
                            AddHoistedCommand(directiveCommand);
                        }
                        else
                        {
                            commands.Add(directiveCommand);
                        }

                        break;

                    case LanguageNode languageNode:
                        commands.Add(new SubmitCode(languageNode));

                        break;

                    case PolyglotSubmissionNode polyglotSubmissionNode:
                        break;
                    
                    case SyntaxNode syntaxNode:
                        break;
                    
                    case DirectiveToken directiveToken:
                        break;
                    
                    case LanguageToken languageToken:
                        break;
                    
                    case DirectiveArgsToken directiveArgsToken:
                        break;
                    
                    case TriviaToken triviaToken:
                        break;
                    
                    case SyntaxToken syntaxToken:
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException(nameof(node));
                }
            }

            return commands;

            void AddHoistedCommand(IKernelCommand command)
            {
                commands.Insert(hoistedCommandsIndex++, command);
            }
        }

        public IReadOnlyList<IKernelCommand> SplitSubmission_Old(SubmitCode originalSubmitCode)
        {
            var directiveParser = GetDirectiveParser();

            var lines = new Queue<string>(
                originalSubmitCode.Code.Split(new[] { "\r\n", "\n" },
                                              StringSplitOptions.None));

            var linesToForward = new List<string>();
            var commands = new List<IKernelCommand>();
            var packageCommands = new List<IKernelCommand>();
            var commandWasSplit = false;

            while (lines.Count > 0)
            {
                var currentLine = lines.Dequeue();

                if (currentLine.TrimStart().StartsWith("#"))
                {
                    var parseResult = directiveParser.Parse(currentLine);

                    if (parseResult.Errors.Count == 0)
                    {
                        commandWasSplit = true;

                        if (AccumulatedSubmission() is { } cmd)
                        {
                            commands.Add(cmd);
                        }

                        var runDirective = new DirectiveCommand(parseResult);

                        if (parseResult.CommandResult.Command.Name == "#r")
                        {
                            var value = parseResult.CommandResult.GetArgumentValueOrDefault<PackageReferenceOrFileInfo>("package");

                            if (value.Value is FileInfo)
                            {
                                linesToForward.Add(currentLine);
                            }
                            else
                            {
                                packageCommands.Add(runDirective);
                            }
                        }
                        else if (parseResult.CommandResult.Command.Name == "#i")
                        {
                            packageCommands.Add(runDirective);
                        }
                        else
                        {
                            commands.Add(runDirective);
                        }
                    }
                    else
                    {
                        if (parseResult.CommandResult.Command == parseResult.Parser.Configuration.RootCommand)
                        {
                            linesToForward.Add(currentLine);
                        }
                        else
                        {
                            commands.Clear();
                            commands.Add(
                                new AnonymousKernelCommand((kernelCommand, context) =>
                                {
                                    var message =
                                        string.Join(Environment.NewLine,
                                                    parseResult.Errors
                                                               .Select(e => e.ToString()));

                                    context.Fail(message: message);
                                    return Task.CompletedTask;
                                }));
                        }
                    }
                }
                else
                {
                    linesToForward.Add(currentLine);
                }
            }

            if (commandWasSplit)
            {
                if (AccumulatedSubmission() is { } newSubmitCode)
                {
                    commands.Add(newSubmitCode);
                }
            }
            else
            {
                commands.Add(originalSubmitCode);
            }

            if (packageCommands.Count > 0)
            {
                var parseResult = directiveParser.Parse("#!nuget-restore");

                packageCommands.Add(new DirectiveCommand(parseResult));
            }

            return packageCommands.Concat(commands).ToArray();

            SubmitCode AccumulatedSubmission()
            {
                if (linesToForward.Any())
                {
                    var code = string.Join(Environment.NewLine, linesToForward);

                    linesToForward.Clear();

                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        return new SubmitCode(code);
                    }
                }

                return null;
            }
        }

        internal IDictionary<string, Func<Parser>> GetSubkernelDirectiveParsers()
        {
            if (!(_kernel is CompositeKernel compositeKernel))
            {
                return null;
            }

            return compositeKernel
                   .ChildKernels
                   .OfType<KernelBase>()
                   .ToDictionary(
                       child => child.Name,
                       child => new Func<Parser>(() => child.SubmissionParser.GetDirectiveParser()));
        }

        internal Parser GetDirectiveParser()
        {
            if (_directiveParser == null)
            {
                EnsureRootCommandIsInitialized();

                var commandLineBuilder =
                    new CommandLineBuilder(_rootCommand)
                        .ParseResponseFileAs(ResponseFileHandling.Disabled)
                        .UseTypoCorrections()
                        .UseHelpBuilder(bc => new HelpBuilderThatOmitsRootCommandName(bc.Console, _rootCommand.Name))
                        .UseHelp()
                        .UseMiddleware(
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
            if (command == null)
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

            _directiveParser = null;
        }

        private void EnsureRootCommandIsInitialized()
        {
            if (_rootCommand == null)
            {
                _rootCommand = new RootCommand();
            }
        }

        private class HelpBuilderThatOmitsRootCommandName : HelpBuilder
        {
            private readonly string _rootCommandName;

            public HelpBuilderThatOmitsRootCommandName(IConsole console, string rootCommandName) : base(console)
            {
                _rootCommandName = rootCommandName;
            }

            public override void Write(ICommand command)
            {
                var capturingConsole = new TestConsole();
                new HelpBuilder(capturingConsole).Write(command);
                Console.Out.Write(
                    capturingConsole.Out
                                    .ToString()
                                    .Replace(_rootCommandName + " ", ""));
            }
        }
    }
}