﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands
{
    internal class DirectiveCommand : KernelCommand
    {
        internal DirectiveCommand(
            ParseResult parseResult,
            KernelCommand parent,
            DirectiveNode directiveNode = null) : base(null, parent)
        {
            ParseResult = parseResult;
            DirectiveNode = directiveNode;
            KernelUri = directiveNode?.KernelUri;
        }

        public ParseResult ParseResult { get; }

        public DirectiveNode DirectiveNode { get; }

        public override async Task InvokeAsync(KernelInvocationContext context)
        {
            if (ParseResult.Errors.Any())
            {
                throw new InvalidOperationException($"{string.Join(";", ParseResult.Errors)}");
            }

            await ParseResult.InvokeAsync();
        }

        public override string ToString()
        {
            return $"Directive: {ParseResult.CommandResult.Command.Name}";
        }
    }
}