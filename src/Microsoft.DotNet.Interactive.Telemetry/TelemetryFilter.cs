// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Telemetry
{
    public sealed class TelemetryFilter : ITelemetryFilter
    {
        private readonly Func<string, string> _hash;

        public TelemetryFilter(Func<string, string> hash)
        {
            _hash = hash ?? throw new ArgumentNullException(nameof(hash));
        }

        public IEnumerable<ApplicationInsightsEntryFormat> Filter(object objectToFilter)
        {
            var plainTextProperties = new HashSet<string>
            {
                "frontend"
            };

            if (objectToFilter == null)
            {
                return new List<ApplicationInsightsEntryFormat>();
            }

            var result = new List<ApplicationInsightsEntryFormat>();

            if (objectToFilter is ParseResult parseResult)
            {
                var mainCommand =
                    // The first command will in the tokens collection will be our main command.
                    parseResult.Tokens?.FirstOrDefault(x => x.Type == TokenType.Command);
                var mainCommandName = mainCommand?.Value;

                var tokens =
                    parseResult.Tokens
                               // skip directives as we do not care right now
                               ?.Where(x => x.Type != TokenType.Directive)
                               .SkipWhile(x => x != mainCommand)
                               // We skip one to not include the main command as part of the collection we want to filter.
                               .Skip(1);

                var entryItems = FilterCommand(mainCommandName, tokens, parseResult.CommandResult, parseResult.Directives);

                if (entryItems != null)
                {
                    result.Add(CreateEntry(entryItems));
                }

            }

            return result.Select(r => r.WithAppliedToPropertiesValue(_hash, name => !plainTextProperties.Contains(name))).ToList();
        }

        private ImmutableArray<KeyValuePair<string, string>>? 
            FilterCommand(string commandName, IEnumerable<Token> tokens, CommandResult commandResult, IDirectiveCollection directives)
        {
            if (commandName == null || tokens == null)
            {
                return null;
            }

            return Rules
                .Select(rule => rule.CommandName == commandName 
                    ? TryMatchRule(rule, tokens, commandResult, directives) 
                    : null)
                .FirstOrDefault(x => x != null);
        }

        /// <summary>
        /// Tries to see if the tokens follow or match the specified command rule.
        /// </summary>
        ImmutableArray<KeyValuePair<string, string>>? 
            TryMatchRule(CommandRule rule, IEnumerable<Token> tokens, CommandResult commandResult, IDirectiveCollection directives)
        {
            var entryItems = ImmutableArray.CreateBuilder<KeyValuePair<string, string>>();
            entryItems.Add(new KeyValuePair<string, string>("verb", rule.CommandName));

            // Filter out option tokens as we query the command result for them when processing a rule.
            var tokenQueue = new Queue<Token>(tokens.Where(x => x.Type != TokenType.Option));
            Token NextToken()
            {
                return tokenQueue.TryDequeue(out var firstToken) ? firstToken : null;
            }

            var currentToken = NextToken();

            // We have a valid rule so far.
            var passed = true;

            foreach (var directive in directives)
            {
                switch (directive.Key)
                {
                    case "vscode":
                    case "jupyter":
                    case "synapse":
                        entryItems.Add(new KeyValuePair<string, string>("frontend", directive.Key));
                        break;
                }
            }

            foreach (var item in rule.Items)
            {
                // Stop checking items since our rule already failed.
                if (!passed)
                {
                    break;
                }

                switch (item)
                {
                    case OptionItem optItem:
                        {
                            var optionValue = commandResult.OptionResult(optItem.Option).GetValueOrDefault()?.ToString();
                            if (optionValue != null && optItem.Values.Contains(optionValue))
                            {
                                entryItems.Add(new KeyValuePair<string, string>(optItem.EntryKey, optionValue));
                            }
                            else
                            {
                                passed = false;
                            }
                            break;
                        }
                    case ArgumentItem argItem:
                        {
                            if (argItem.TokenType == currentToken?.Type &&
                                argItem.Value == currentToken?.Value)
                            {
                                entryItems.Add(new KeyValuePair<string, string>(argItem.EntryKey, argItem.Value));
                                currentToken = NextToken();
                            }
                            else if (argItem.IsOptional)
                            {
                                currentToken = NextToken();
                            }
                            else
                            {
                                passed = false;
                            }
                            break;
                        }
                    case IgnoreItem ignoreItem:
                        {
                            if (ignoreItem.TokenType == currentToken?.Type)
                            {
                                currentToken = NextToken();
                            }
                            else if (ignoreItem.IsOptional)
                            {
                                currentToken = NextToken();
                            }
                            else
                            {
                                passed = false;
                            }
                            break;
                        }
                    default:
                        passed = false;
                        break;
                }
            }

            if (passed)
            {
                return entryItems.ToImmutable();
            }
            else
            {
                return null;
            }
        }

        private ApplicationInsightsEntryFormat CreateEntry(IEnumerable<KeyValuePair<string, string>> entryItems)
        {
            return new ApplicationInsightsEntryFormat("command", new Dictionary<string, string>(entryItems));
        }

        private abstract class CommandRuleItem
        {
        }

        private class OptionItem : CommandRuleItem
        {
            public OptionItem(string option, string[] values, string entryKey)
            {
                Option = option;
                Values = values.ToImmutableArray();
                EntryKey = entryKey;
            }

            public string Option { get; }
            public ImmutableArray<string> Values { get; }
            public string EntryKey { get; }
        }

        private class ArgumentItem : CommandRuleItem
        {
            public ArgumentItem(string value, TokenType type, string entryKey, bool isOptional)
            {
                Value = value;
                TokenType = type;
                EntryKey = entryKey;
                IsOptional = isOptional;
            }

            public string Value { get; }
            public TokenType TokenType { get; }
            public string EntryKey { get; }
            public bool IsOptional { get; }
        }

        private class IgnoreItem : CommandRuleItem
        {
            public IgnoreItem(TokenType type, bool isOptional)
            {
                TokenType = type;
                IsOptional = isOptional;
            }

            public TokenType TokenType { get; }
            public bool IsOptional { get; }
        }

        private class CommandRule
        {
            public CommandRule(string commandName, IEnumerable<CommandRuleItem> items)
            {
                CommandName = commandName;
                Items = ImmutableArray.CreateRange(items);
            }

            public string CommandName { get; }
            public ImmutableArray<CommandRuleItem> Items { get; }
        }

        private static CommandRuleItem Option(string option, string[] values, string entryKey)
        {
            return new OptionItem(option, values, entryKey);
        }

        private static CommandRuleItem Arg(string value, TokenType type, string entryKey, bool isOptional)
        {
            return new ArgumentItem(value, type, entryKey, isOptional);
        }

        private static CommandRuleItem Ignore(TokenType type, bool isOptional)
        {
            return new IgnoreItem(type, isOptional);
        }

        private static CommandRule[] Rules => new[]
        {
            new CommandRule("jupyter",
                new[]{
                    Arg("install", TokenType.Command, "subcommand", isOptional: false) }),

            new CommandRule("jupyter",
                new[]{
                    Option("--default-kernel", new[]{ "csharp", "fsharp", "powershell" }, "default-kernel"),
                    Ignore(TokenType.Argument, isOptional: true) // connection file
                }),

            new CommandRule("stdio",
                new[]{
                    Option("--default-kernel", new[]{ "csharp", "fsharp", "powershell" }, "default-kernel")
                })
        };
    }
}
