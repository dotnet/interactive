// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Linq;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive
{
    internal class SubmissionSplitter
    {
        private Parser _directiveParser;

        private readonly List<Command> _directiveCommands = new List<Command>();

        public IReadOnlyCollection<ICommand> Directives => _directiveCommands;

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

                    var runDirective = new AnonymousKernelCommand(
                        (_, __) => parseResult.InvokeAsync());

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

                        commands.Add(new DisplayError(message));
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

                hoistedCommands.Add(
                    new AnonymousKernelCommand(
                        (_, __) => parseResult.InvokeAsync()));
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
                var root = new RootCommand();

                foreach (var c in _directiveCommands)
                {
                    root.Add(c);
                }

                var commandLineBuilder =
                    new CommandLineBuilder(root)
                        .ParseResponseFileAs(ResponseFileHandling.Disabled)
                        .UseMiddleware(
                            context => context.BindingContext
                                              .AddService(
                                                  typeof(KernelInvocationContext),
                                                  () => KernelInvocationContext.Current));

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

            if (!command.Name.StartsWith("#") &&
                !command.Name.StartsWith("%"))
            {
                throw new ArgumentException("Directives must begin with # or %");
            }

            _directiveCommands.Add(command);
            _directiveParser = null;
        }
    }
}