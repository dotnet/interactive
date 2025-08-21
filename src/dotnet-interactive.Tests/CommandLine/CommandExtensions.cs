using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using Microsoft.DotNet.Interactive.App.Tests.Extensions;

namespace Microsoft.DotNet.Interactive.App.Tests.CommandLine;

public static class CommandExtensions
{
    /// <summary>
    /// Throws an exception if the parser configuration is ambiguous or otherwise not valid.
    /// </summary>
    /// <remarks>Due to the performance cost of this method, it is recommended to be used in unit testing or in scenarios where the parser is configured dynamically at runtime.</remarks>
    /// <exception cref="InvalidOperationException">Thrown if the configuration is found to be invalid.</exception>
    public static void ThrowIfInvalid(this Command command)
    {
        if (command.Parents.FlattenBreadthFirst(c => c.Parents).Any(ancestor => ancestor == command))
        {
            throw new InvalidOperationException($"Cycle detected in command tree. Command '{command.Name}' is its own ancestor.");
        }

        int count = command.Subcommands.Count + command.Options.Count;
        for (var i = 0; i < count; i++)
        {
            Symbol symbol1 = GetChild(i, command, out HashSet<string> aliases1);

            for (var j = i + 1; j < count; j++)
            {
                Symbol symbol2 = GetChild(j, command, out HashSet<string> aliases2);

                if (symbol1.Name.Equals(symbol2.Name, StringComparison.Ordinal)
                    || aliases1 is not null && aliases1.Contains(symbol2.Name))
                {
                    throw new InvalidOperationException($"Duplicate alias '{symbol2.Name}' found on command '{command.Name}'.");
                }
                else if (aliases2 is not null && aliases2.Contains(symbol1.Name))
                {
                    throw new InvalidOperationException($"Duplicate alias '{symbol1.Name}' found on command '{command.Name}'.");
                }

                if (aliases1 is not null && aliases2 is not null)
                {
                    // take advantage of the fact that we are dealing with two hash sets
                    if (aliases1.Overlaps(aliases2))
                    {
                        foreach (string symbol2Alias in aliases2)
                        {
                            if (aliases1.Contains(symbol2Alias))
                            {
                                throw new InvalidOperationException($"Duplicate alias '{symbol2Alias}' found on command '{command.Name}'.");
                            }
                        }
                    }
                }
            }

            if (symbol1 is Command childCommand)
            {
                childCommand.ThrowIfInvalid();
            }
        }

        static Symbol GetChild(int index, Command command, out HashSet<string> aliases)
        {
            if (index < command.Subcommands.Count)
            {
                aliases = command.Subcommands[index].Aliases.ToHashSet();
                return command.Subcommands[index];
            }

            aliases = command.Options[index - command.Subcommands.Count].Aliases.ToHashSet();
            return command.Options[index - command.Subcommands.Count];
        }
    }
}