// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Parsing
{
    public class DirectiveParseResult
    {
        private ParseResult _commandLineParseResult;
        public ParseResult ParseResult => _commandLineParseResult;
        public DirectiveParser Parser { get; }
        public DirectiveParseResult(ParseResult commandLineParseResult, DirectiveParser parser)
            => (Parser, _commandLineParseResult) = (parser, commandLineParseResult);

        public IReadOnlyCollection<ParseError> Errors { get; init; }
        public IReadOnlyList<string> UnmatchedTokens { get; init; }
        public IReadOnlyList<string> UnparsedTokens => _commandLineParseResult.UnparsedTokens;
        public IReadOnlyList<Token> Tokens => _commandLineParseResult.Tokens;
        public CommandResult CommandResult => _commandLineParseResult.CommandResult;

        public IEnumerable<string> GetSuggestions(int requestPosition) => _commandLineParseResult.GetSuggestions(requestPosition);
        public async Task<int> InvokeAsync() => await _commandLineParseResult.InvokeAsync();
        public T ValueForArgument<T>(string name) => _commandLineParseResult.ValueForArgument<T>(name);
    }

    public class DirectiveParser
    {
        private readonly Parser _commandLineParser;
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

        public DirectiveParseResult Parse(string input)
        {
            var parseResult = _commandLineParser.Parse(input);
            IReadOnlyList<string> unmatchedTokens;
            IReadOnlyCollection<ParseError> errors;
            if (parseResult.UnmatchedTokens.Count == parseResult.Errors.Count && parseResult.UnmatchedTokens[0].StartsWith("//"))
            {
                unmatchedTokens = new List<string>();
                errors = new List<ParseError>();
            }
            else
            {
                unmatchedTokens = parseResult.UnmatchedTokens;
                errors = parseResult.Errors;

            }
            return new DirectiveParseResult(parseResult, this)
            {
                UnmatchedTokens = unmatchedTokens,
                Errors = errors
            };
        }

        public CommandLineConfiguration Configuration => _commandLineParser.Configuration;
    }
}
