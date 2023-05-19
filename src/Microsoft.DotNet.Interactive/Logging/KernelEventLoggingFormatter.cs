// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive;

internal sealed class KernelEventLoggingFormatter : LoggingFormatter<KernelEvent>
{
    internal KernelEventLoggingFormatter() : base(FormatKernelEvent)
    {
    }

    private static bool FormatKernelEvent(KernelEvent @event, FormatContext context)
    {
        if (@event is null)
        {
            context.Writer.Write($"{nameof(KernelEvent)}: <null>");
        }
        else
        {
            context.Writer.Write(@event.GetType().Name);
            context.Writer.Write(' ');

            switch (@event)
            {
                case CodeSubmissionReceived codeSubmissionReceived:
                    context.Writer.Write(codeSubmissionReceived.Code.TruncateForDisplay());
                    break;

                case CompleteCodeSubmissionReceived completeCodeSubmissionReceived:
                    context.Writer.Write(completeCodeSubmissionReceived.Code.TruncateForDisplay());
                    break;

                case CommandFailed commandFailed:
                    context.Writer.Write(commandFailed.Message);
                    break;

                case DiagnosticsProduced diagnosticsProduced:
                    var diagnostics = diagnosticsProduced.Diagnostics;
                    if (diagnostics.Any()) // TODO: How come we produce empty DiagnosticsProduced events?
                    {
                        var firstMessage = diagnostics.First().Message;
                        context.Writer.Write(firstMessage);

                        var count = diagnostics.Count;
                        if (count > 1)
                        {
                            context.Writer.Write($" (and {count - 1} more)");
                        }
                    }
                    break;

                case DiagnosticLogEntryProduced diagnosticLogEntryProduced:
                    context.Writer.Write(diagnosticLogEntryProduced.Message);
                    break;

                case DisplayEvent displayEvent:
                    context.Writer.Write(displayEvent.Value?.ToString().TruncateForDisplay());
                    break;

                case PackageAdded packageAdded:
                    context.Writer.Write(packageAdded.PackageReference);
                    break;

                default:
                    break;
            }

            context.Writer.Write($" [Command: {@event.Command.ToDisplayString(MimeTypes.Logging)}]");
        }

        return true;
    }
}
