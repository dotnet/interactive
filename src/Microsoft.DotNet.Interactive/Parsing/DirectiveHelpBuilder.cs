// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

        public DirectiveHelpBuilder(IConsole console, string rootCommandName) : base(new SystemConsole())
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

        private class SystemConsole : IConsole
        {
            public IStandardStreamWriter Out { get; } = new StandardOutStreamWriter();

            public bool IsOutputRedirected => System.Console.IsOutputRedirected;

            public IStandardStreamWriter Error { get; } = new StandardErrorStreamWriter();

            public bool IsErrorRedirected => System.Console.IsErrorRedirected;

            public bool IsInputRedirected => System.Console.IsInputRedirected;

            private class StandardOutStreamWriter : IStandardStreamWriter
            {
                public void Write(string value) => System.Console.Out.Write(value);
            }

            private class StandardErrorStreamWriter : IStandardStreamWriter
            {
                public void Write(string value) => System.Console.Error.Write(value);
            }
        }
    }
}