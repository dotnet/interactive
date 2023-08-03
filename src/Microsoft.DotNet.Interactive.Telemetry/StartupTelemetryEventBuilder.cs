// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Telemetry;

public sealed class StartupTelemetryEventBuilder
{
    private readonly Func<string, string> _hash;
    private readonly HashSet<string> _clearTextProperties = new(new[] { "frontend" });

    public StartupTelemetryEventBuilder(Func<string, string> hash)
    {
        _hash = hash ?? throw new ArgumentNullException(nameof(hash));

    }

    public IEnumerable<TelemetryEvent> GetTelemetryEventsFrom(ParseResult parseResult)
    {
        if (parseResult is null)
        {
            return Array.Empty<TelemetryEvent>();
        }

        var result = new List<TelemetryEvent>();

        var mainCommand =
            // The first command will in the tokens collection will be our main command.
            parseResult.Tokens.FirstOrDefault(x => x.Type == TokenType.Command);

        var mainCommandName = mainCommand?.Value;

        var tokens =
            parseResult.Tokens
                       // skip directives as we do not care right now
                       .Where(x => x.Type != TokenType.Directive)
                       .SkipWhile(x => x != mainCommand)
                       // We skip one to not include the main command as part of the collection we want to filter.
                       .Skip(1);

        var entryItems = Rules
                         .Select(rule => rule.CommandName == mainCommandName
                                             ? TryMatchRule(rule)
                                             : null)
                         .FirstOrDefault(x => x is not null);

        if (entryItems is not null)
        {
            result.Add(CreateEntry(entryItems));
        }

        return result.Select(r =>
                     {
                         var appliedProperties = r.Properties.ToDictionary(
                             p => p.Key,
                             p => !_clearTextProperties.Contains(p.Key)
                                      ? _hash(p.Value)
                                      : p.Value);
                         return new TelemetryEvent(r.EventName, appliedProperties, r.Metrics);
                     })
                     .ToList();

        ImmutableArray<KeyValuePair<string, string>>? TryMatchRule(CommandRule rule)
        {
            var entryItems = ImmutableArray.CreateBuilder<KeyValuePair<string, string>>();
            entryItems.Add(new KeyValuePair<string, string>("verb", rule.CommandName));

            // Filter out option tokens as we query the command result for them when processing a rule.
            var tokenQueue = new Queue<Token>(tokens.Where(x => x.Type != TokenType.Option));
            var currentToken = NextToken();

            // We have a valid rule so far.
            var passed = true;

            var commandResult = parseResult.CommandResult;

            var frontendName = GetFrontendName(parseResult.Directives, parseResult.CommandResult);
            entryItems.Add(new KeyValuePair<string, string>("frontend", frontendName));

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
                        var optionValue = commandResult.Children.OfType<OptionResult>().FirstOrDefault(o => o.Option.HasAlias(optItem.Option))?.GetValueOrDefault()?.ToString();
                        if (optionValue is not null && optItem.Values.Contains(optionValue))
                        {
                            entryItems.Add(new KeyValuePair<string, string>(optItem.EntryKey, optionValue));
                        }
                        else
                        {
                            passed = false;
                        }

                        break;

                    case ArgumentItem argItem:
                        if (argItem.TokenType == currentToken.Type &&
                            argItem.Value == currentToken.Value)
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

                    case IgnoreItem ignoreItem:
                        if (ignoreItem.TokenType == currentToken.Type)
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

            Token NextToken()
            {
                return tokenQueue.TryDequeue(out var firstToken) ? firstToken : default;
            }
        }
    }

    private TelemetryEvent CreateEntry(IEnumerable<KeyValuePair<string, string>> entryItems)
    {
        return new TelemetryEvent("command", new Dictionary<string, string>(entryItems));
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

    private static string GetFrontendName(
        DirectiveCollection directives,
        CommandResult commandResult)
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CODESPACES")))
        {
            return "gitHubCodeSpaces";
        }

        foreach (var directive in directives)
        {
            switch (directive.Key)
            {
                case "jupyter":
                case "synapse":
                case "vscode":
                    return directive.Key;
            }

            if (directive.Key.StartsWith("vs") &&
                int.TryParse(directive.Key.Substring(2), out _)) // VS appends the process id after the "vs" prefix.
            {
                return "vs";
            }
        }

        switch (commandResult.Command.Name)
        {
            case "jupyter":
                return commandResult.Command.Name;
        }

        var frontendName = Environment.GetEnvironmentVariable("DOTNET_INTERACTIVE_FRONTEND_NAME");

        if (string.IsNullOrWhiteSpace(frontendName))
        {
            frontendName = "unknown";
        }

        return frontendName;
    }
}