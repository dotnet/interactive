// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Parsing;

public class DirectiveHelpBuilder : HelpBuilder
{
    private readonly string _rootCommandName;
    private readonly Dictionary<Symbol, string> _directiveHelp = new();

    public DirectiveHelpBuilder(string rootCommandName) : base(System.CommandLine.LocalizationResources.Instance)
    {
        _rootCommandName = rootCommandName;
    }

    public override void Write(HelpContext context)
    {
        using var writer = new StringWriter();
        new HelpBuilder(System.CommandLine.LocalizationResources.Instance).Write(context.Command, writer);
        var cleanedUp = CleanUp(writer.ToString());
        context.Output.Write(cleanedUp);
    }

    public string GetHelpForSymbol(Symbol symbol)
    {
        if (_directiveHelp.TryGetValue(symbol, out var help))
        {
            return help;
        }

        using var writer = new StringWriter();

        switch (symbol)
        {
            case Command command:
                var context = new HelpContext(this, command, writer);

                Write(context);
                break;

            case Option option:
                var parentCommand = option.Parents.OfType<Command>().FirstOrDefault();

                if (parentCommand is not null)
                {
                    var ctx = new HelpContext(this, parentCommand, writer);
                    var helpRow = GetTwoColumnRow(option, ctx);
                    writer.WriteLine($"{helpRow.FirstColumnText} {helpRow.SecondColumnText}");
                }
                break;
        }

        help = CleanUp(writer.ToString());

        _directiveHelp[symbol] = help;

        return help;
    }

    private string CleanUp(string value) =>
        value.Replace(_rootCommandName + " ", "");
}