// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.IO;

namespace Microsoft.DotNet.Interactive.Parsing
{
    public class DirectiveHelpBuilder : HelpBuilder
    {
        private readonly string _rootCommandName;
        private readonly Dictionary<ISymbol, string> _directiveHelp = new Dictionary<ISymbol, string>();

        public DirectiveHelpBuilder(IConsole console, string rootCommandName) : base(console)
        {
            _rootCommandName = rootCommandName;
        }

        public override void Write(ICommand command)
        {
            var capturingConsole = new TestConsole();
            new HelpBuilder(capturingConsole).Write(command);
            Console.Out.Write(
                CleanUp(capturingConsole));
        }

        public string GetHelpForSymbol(ISymbol symbol)
        {
            if (_directiveHelp.TryGetValue(symbol, out var help))
            {
                return help;
            }

            var console = new TestConsole();
            var helpBuilder = new HelpBuilder(console);

            switch (symbol)
            {
                case ICommand command:
                    helpBuilder.Write(command);
                    break;
                case IOption option:
                    helpBuilder.Write(option);
                    break;
            }

            help = CleanUp(console);

            _directiveHelp[symbol] = help;

            return help;
        }

        private string CleanUp(TestConsole capturingConsole) =>
            capturingConsole.Out
                            .ToString()
                            .Replace(_rootCommandName + " ", "");
    }
}