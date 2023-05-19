// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive;

internal sealed class KernelCommandLoggingFormatter : LoggingFormatter<KernelCommand>
{
    internal KernelCommandLoggingFormatter() : base(FormatKernelCommand)
    {
    }

    private static bool FormatKernelCommand(KernelCommand command, FormatContext context)
    {
        if (command is null)
        {
            context.Writer.Write($"{nameof(KernelCommand)}: <null>");
        }
        else
        {
            context.Writer.Write(command.GetType().Name);
            context.Writer.Write(' ');

            switch (command)
            {
                case SubmitCode submitCode:
                    context.Writer.Write(submitCode.Code.TruncateForDisplay());
                    break;

                case DirectiveCommand directiveCommand:
                    context.Writer.Write(directiveCommand.ParseResult.CommandResult.Command.Name);
                    break;

                default:
                    break;
            }

            context.Writer.Write($" [Token: {command.GetOrCreateToken()}]");
        }

        return true;
    }
}
