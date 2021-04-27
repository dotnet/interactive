// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

namespace Microsoft.DotNet.Interactive.Parsing
{
    internal class DirectiveParser : Parser
    {
        private Parser _commandLineParser;
        public DirectiveParser(RootCommand rootCommand)
        {
            var commandLineBuilder =
                    new CommandLineBuilder(rootCommand)
                        .ParseResponseFileAs(ResponseFileHandling.Disabled)
                        .UseTypoCorrections()
                        .UseHelpBuilder(bc => new DirectiveHelpBuilder(rootCommand.Name))
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
            _commandLineParser = commandLineBuilder.Build();
        }

        public ParseResult Parse(string input)
        {
            if (FindComment(input) is { } realPos)
                input = input.Substring(0, realPos);
            return _commandLineParser.Parse(input);
        }

        private static int? FindComment(string input)
        {
            var firstQuote = input.IndexOf('"');
            if (firstQuote is -1)
                return null;

            var secondQuote = input.IndexOf('"', firstQuote + 1);
            if (secondQuote is -1)
                return null;

            var doubleSlash = input.IndexOf("//", secondQuote + 1);
            if (doubleSlash is -1)
                return null;

            return doubleSlash;
        }
    }
}
