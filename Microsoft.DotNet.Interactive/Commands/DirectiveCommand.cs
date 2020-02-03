﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Commands
{
    internal class DirectiveCommand : KernelCommandBase
    {
        public DirectiveCommand(ParseResult parseResult)
        {
            ParseResult = parseResult;
        }

        public ParseResult ParseResult { get; }
        
        public override async Task InvokeAsync(KernelInvocationContext context)
        {
            await ParseResult.InvokeAsync();
        }

        public override string ToString()
        {
            return $"Directive: {ParseResult.CommandResult.Command.Name}";
        }
    }
}