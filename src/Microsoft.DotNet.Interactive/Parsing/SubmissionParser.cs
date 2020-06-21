// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
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
        public string KernelLanguage { get; internal set; }

        public SubmissionParser(KernelBase kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            KernelLanguage = kernel switch
            {
                CompositeKernel c => c.DefaultKernelName,
                _ => kernel.Name
            };
        }

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

        public IReadOnlyList<IKernelCommand> SplitSubmission(SubmitCode submitCode) 
        {
            var commands = new List<IKernelCommand>();
            var nugetRestoreOnKernels = new HashSet<string>();
            var hoistedCommandsIndex = 0;

            var tree = Parse(submitCode.Code, submitCode.TargetKernelName);
            var nodes = tree.GetRoot().ChildNodes.ToArray();
            var targetKernelName = submitCode.TargetKernelName ?? KernelLanguage;

            foreach (var node in nodes)
            {
                switch (node)
                {
                    case DirectiveNode directiveNode:
                        var parseResult = directiveNode.GetDirectiveParseResult();

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
                                }, parent: submitCode.Parent));
                            break;
                        }

                        var directiveCommand = new DirectiveCommand(
                            parseResult,
                            submitCode.Parent,
                            directiveNode);

                        if (directiveNode is KernelNameDirectiveNode kernelNameNode)
                        {
                            targetKernelName = kernelNameNode.KernelName;
                        }

                        if (parseResult.CommandResult.Command.Name == "#r")
                        {
                            var value = parseResult.CommandResult.GetArgumentValueOrDefault<PackageReferenceOrFileInfo>("package");

                            if (value.Value is FileInfo)
                            {
                                AddHoistedCommand(
                                    new SubmitCode(
                                        directiveNode,
                                        submitCode.SubmissionType,
                                        submitCode.Parent));
                            }
                            else
                            {
                                AddHoistedCommand(directiveCommand);
                                nugetRestoreOnKernels.Add(targetKernelName);
                            }
                        }
                        else if (parseResult.CommandResult.Command.Name == "#i")
                        {
                            directiveCommand.TargetKernelName = targetKernelName;
                            AddHoistedCommand(directiveCommand);
                        }
                        else
                        {
                            commands.Add(directiveCommand);
                        }

                        break;

                    case LanguageNode languageNode:
                        commands.Add(new SubmitCode(
                                         languageNode,
                                         submitCode.SubmissionType,
                                         submitCode.Parent));
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException(nameof(node));
                }
            }

            foreach (var kernelName in nugetRestoreOnKernels)
            {
                var findKernel = _kernel.FindKernel(kernelName);

                if (findKernel is KernelBase kernelBase &&
                    kernelBase.SubmissionParser.GetDirectiveParser() is {} parser)
                {
                    var restore = new DirectiveCommand(
                        parser.Parse("#!nuget-restore"),
                        submitCode.Parent);
                    AddHoistedCommand(restore);
                }
            }

            if (NoSplitWasNeeded(out var originalSubmission))
            {
                return originalSubmission;
            }

            var parent = submitCode.Parent ?? submitCode;

            foreach (var command in commands.OfType<KernelCommandBase>())
            {
                command.Parent = parent;
            }

            return commands;

            void AddHoistedCommand(IKernelCommand command)
            {
                commands.Insert(hoistedCommandsIndex++, command);
            }

            bool NoSplitWasNeeded(out IReadOnlyList<IKernelCommand> splitSubmission)
            {
                if (commands.Count == 0)
                {
                    splitSubmission = new[] { submitCode };
                    return true;
                }

                if (commands.Count == 1)
                {
                    if (commands[0] is SubmitCode sc)
                    {
                        if (submitCode.Code.Equals(sc.Code, StringComparison.Ordinal))
                        {
                            splitSubmission = new[] { submitCode };
                            return true;
                        }
                    }
                }

                splitSubmission = null;
                return false;
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
                        .UseHelpBuilder(bc => new DirectiveHelpBuilder(bc.Console, _rootCommand.Name))
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

        public static CompletionItem CompletionItemFor(string name, ParseResult parseResult)
        {
            var symbol = parseResult.CommandResult
                                    .Command
                                    .Children
                                    .GetByAlias(name);

            var kind = symbol switch
            {
                IArgument _ => "Value",
                IOption _ => "Property",
                ICommand _ => "Method",
                _ => null
            };

            var helpBuilder = new DirectiveHelpBuilder(
                new TestConsole(),
                parseResult.Parser.Configuration.RootCommand.Name);

            return new CompletionItem(
                displayText: name,
                kind: kind,
                filterText: name,
                sortText: name,
                insertText: name,
                documentation: helpBuilder.GetHelpForSymbol(symbol));
        }

        private void EnsureRootCommandIsInitialized()
        {
            if (_rootCommand == null)
            {
                _rootCommand = new RootCommand();
            }
        }
    }
}