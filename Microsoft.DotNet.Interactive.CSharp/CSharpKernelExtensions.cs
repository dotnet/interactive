// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.CSharp
{
    public static class CSharpKernelExtensions
    {
        public static CSharpKernel UseDefaultFormatting(
            this CSharpKernel kernel)
        {
            var command = new SubmitCode($@"
using static {typeof(PocketViewTags).FullName};
using {typeof(PocketView).Namespace};
");

            kernel.DeferCommand(command);

            return kernel;
        }

        public static CSharpKernel UseKernelHelpers(
            this CSharpKernel kernel)
        {
            var command = new SubmitCode($@"
using static {typeof(Kernel).FullName};
");

            kernel.DeferCommand(command);

            return kernel;
        }

        public static CSharpKernel UseWho(this CSharpKernel kernel)
        {
            kernel.AddDirective(who_and_whos());
            Formatter.Register(new CurrentVariablesFormatter());
            return kernel;
        }

        private static Command who_and_whos()
        {
            var command = new Command("#!whos", "Display the names of the current top-level variables and their values.")
            {
                Handler = CommandHandler.Create((ParseResult parseResult, KernelInvocationContext context) =>
                {
                    var alias = parseResult.CommandResult.Token.Value;

                    var detailed = alias == "#!whos";

                    Display(context, detailed);

                    return Task.CompletedTask;
                })
            };

            // FIX: (who_and_whos) this should be a separate command with separate help
            command.AddAlias("#!who");

            return command;

            void Display(KernelInvocationContext context, bool detailed)
            {
                if (context.Command is SubmitCode &&
                    context.HandlingKernel is CSharpKernel kernel)
                {
                    var variables = kernel.ScriptState.Variables.Select(v => new CurrentVariable(v.Name, v.Type, v.Value));

                    var currentVariables = new CurrentVariables(
                        variables,
                        detailed);

                    var html = currentVariables
                        .ToDisplayString(HtmlFormatter.MimeType);

                    context.Publish(
                        new DisplayedValueProduced(
                            html,
                            context.Command,
                            new[]
                            {
                                new FormattedValue(
                                    HtmlFormatter.MimeType,
                                    html)
                            }));
                }
            }
        }
    }
}