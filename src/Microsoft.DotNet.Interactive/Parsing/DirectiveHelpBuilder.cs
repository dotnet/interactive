// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.IO;

namespace Microsoft.DotNet.Interactive.Parsing
{
    public class DirectiveHelpBuilder : HelpBuilder
    {
        private readonly string _rootCommandName;
        private readonly Dictionary<ISymbol, string> _directiveHelp = new();

        public DirectiveHelpBuilder(string rootCommandName) : base(Resources.Instance)
        {
            _rootCommandName = rootCommandName;
        }

        public override void Write(ICommand command, TextWriter writer)
        {
            var buffer = new StringWriter();
            new HelpBuilder(Resources.Instance).Write(command, buffer);
            var buffer2 = CleanUp(buffer.ToString());
            writer.Write(buffer2);
        }

        public string GetHelpForSymbol(ISymbol symbol)
        {
            if (_directiveHelp.TryGetValue(symbol, out var help))
            {
                return help;
            }

            var writer = new StringWriter();
            var helpBuilder = new HelpBuilder(Resources.Instance);

            switch (symbol)
            {
                case ICommand command:
                    helpBuilder.Write(command, writer);
                    break;
                case IOption option:
                    var helpItem = GetHelpItem(option);

                    writer.WriteLine($"{helpItem.Descriptor} {helpItem.Description}");

                    break;
            }

            help = CleanUp(writer.ToString());

            _directiveHelp[symbol] = help;

            return help;
        }

        private string CleanUp(string outputIncludingRootCommand) =>
            outputIncludingRootCommand.Replace(_rootCommandName + " ", "");
    }
}