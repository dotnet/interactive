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

            var nonDirectiveLines = new List<string>();
            var commands = new List<IKernelCommand>();
            var hoistedCommands = new List<IKernelCommand>();
            var commandWasSplit = false;

            while (lines.Count > 0)
            {
                var currentLine = lines.Dequeue();

                if (string.IsNullOrWhiteSpace(currentLine))
                {
                    nonDirectiveLines.Add(currentLine);
                    continue;
                }

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

                    if (command.Name == "#r")
                    {
                        hoistedCommands.Add(runDirective);
                    }
                    else
                    {
                        commands.Add(runDirective);
                    }
                }
                else
                {
                    if (command == parseResult.Parser.Configuration.RootCommand ||
                        command.Name == "#r")
                    {
                        nonDirectiveLines.Add(currentLine);
                    }
                    else
                    {
                        var message =
                            string.Join(Environment.NewLine,
                                        parseResult.Errors
                                                   .Select(e => e.ToString()));

                        commands.Clear();
                        commands.Add(
                            new AnonymousKernelCommand((kernelCommand, context) =>
                            {
                                 context.Fail(message: message);
                                 return Task.CompletedTask;
                            }));
                    }
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

            if (hoistedCommands.Count > 0)
            {
                var parseResult = directiveParser.Parse("#!nuget-restore");

                hoistedCommands.Add(new DirectiveCommand(parseResult));
            }

            return hoistedCommands.Concat(commands).ToArray();

            IKernelCommand AccumulatedSubmission()
            {
                if (nonDirectiveLines.Any())
                {
                    var code = string.Join(Environment.NewLine, nonDirectiveLines);

                    nonDirectiveLines.Clear();

                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        return new SubmitCode(code);
                    }
                }

                return null;
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
                                           () => KernelInvocationContext.Current);
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