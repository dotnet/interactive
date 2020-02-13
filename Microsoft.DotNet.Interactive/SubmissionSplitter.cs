// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive
{
    internal class SubmissionSplitter
    {
        private Parser _directiveParser;

        private RootCommand _rootCommand;

        public IReadOnlyCollection<ICommand> Directives => _rootCommand?.Children.OfType<ICommand>().ToArray() ?? Array.Empty<ICommand>();

        public IReadOnlyList<IKernelCommand> SplitSubmission(SubmitCode submitCode)
        {
            var directiveParser = GetDirectiveParser();

            var lines = new Queue<string>(
                submitCode.Code.Split(new[] { "\r\n", "\n" },
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
                    var command = parseResult.CommandResult.Command;

                    if (parseResult.Errors.Count == 0)
                    {
                        commandWasSplit = true;

                        if (AccumulatedSubmission() is { } cmd)
                        {
                            commands.Add(cmd);
                        }

                        var runDirective = new DirectiveCommand(parseResult);

                        if (command.Name == "#r" || 
                            command.Name == "#i")
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
                        if (command == parseResult.Parser.Configuration.RootCommand)
                        {
                            linesToForward.Add(currentLine);
                        }
                        else if (IsDirectiveSupportedByCompiler(command, parseResult))
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
                if (AccumulatedSubmission() is { } command)
                {
                    commands.Add(command);
                }
            }
            else
            {
                commands.Add(submitCode);
            }

            if (packageCommands.Count > 0)
            {
                var parseResult = directiveParser.Parse("#!nuget-restore");

                packageCommands.Add(new DirectiveCommand(parseResult));
            }

            return packageCommands.Concat(commands).ToArray();

            IKernelCommand AccumulatedSubmission()
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

        private static bool IsDirectiveSupportedByCompiler(
            ICommand command,
            ParseResult parseResult)
        {
            switch (command.Name)
            {
                case "#r":
                    if (parseResult.Errors.Any(e => e.Message.Contains("nuget:")))
                    {
                        return false;
                    }

                    return true;

                default:
                    return false;
            }
        }

        private Parser GetDirectiveParser()
        {
            if (_directiveParser == null)
            {
                EnsureRootCommandIsInitialized();

                var commandLineBuilder =
                    new CommandLineBuilder(_rootCommand)
                        .ParseResponseFileAs(ResponseFileHandling.Disabled)
                        .UseTypoCorrections()
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
    }
}