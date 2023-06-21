// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

#nullable enable

namespace Pocket;

// ----------------------------------------------------------------------------
// NOTE: The code in this file is shipped as content in a NuGet package and
// utilized by some components that live outside the current repo. It should
// therefore be kept self-sufficient. Avoid referencing any internal types,
// extension methods or helper code defined within other files as much as
// possible.
// ----------------------------------------------------------------------------

internal static partial class Format
{
    static partial void CustomizeLogString(object value, ref string output)
    {
        switch (value)
        {
            case KernelCommand command:
                {
                    using var writer = new StringWriter();
                    writer.AppendLogString(command);
                    output = writer.ToString();
                    break;
                }
            case KernelEvent @event:
                {
                    using var writer = new StringWriter();
                    writer.AppendLogString(@event);
                    output = writer.ToString();
                    break;
                }
            default:
                break;
        }
    }

    private static void AppendLogString(this TextWriter writer, KernelCommand command)
    {
        writer.Write("⁞Ϲ⁞ ");
        writer.Write(command.GetType().Name);
        writer.Write(' ');

        switch (command)
        {
            case ChangeWorkingDirectory changeWorkingDirectory:
                writer.Write(changeWorkingDirectory.WorkingDirectory.TruncateIfNeeded());
                break;

            case DisplayError displayError:
                writer.Write(displayError.Message.TruncateIfNeeded());
                break;

            case DisplayValue displayValue:
                writer.Write('\'');
                writer.Write(displayValue.FormattedValue.Value.TruncateIfNeeded());
                writer.Write("' (");
                writer.Write(displayValue.FormattedValue.MimeType);
                writer.Write(')');
                writer.AppendProperties(
                    (nameof(displayValue.ValueId), displayValue.ValueId));
                break;

            case RequestDiagnostics requestDiagnostics:
                writer.Write(requestDiagnostics.Code.TruncateIfNeeded());
                break;

            case RequestInput requestInput:
                writer.Write(requestInput.Prompt);
                writer.AppendProperties(
                    (nameof(requestInput.ValueName), requestInput.ValueName),
                    (nameof(requestInput.IsPassword), requestInput.IsPassword.ToString()),
                    (nameof(requestInput.InputTypeHint), requestInput.InputTypeHint));
                break;

            case RequestValue requestValue:
                writer.Write(requestValue.Name);
                writer.Write(" (");
                writer.Write(requestValue.MimeType);
                writer.Write(')');
                break;

            case RequestValueInfos requestValueInfos:
                writer.Write(requestValueInfos.MimeType);
                break;

            case SendEditableCode sendEditableCode:
                writer.Write(sendEditableCode.Code.TruncateIfNeeded());
                writer.AppendProperties(
                    (nameof(sendEditableCode.KernelName), sendEditableCode.KernelName));
                break;

            case SendValue sendValue:
                writer.Write(sendValue.Name);
                writer.Write(" '");
                writer.Write(sendValue.FormattedValue.Value.TruncateIfNeeded());
                writer.Write("' (");
                writer.Write(sendValue.FormattedValue.MimeType);
                writer.Write(')');
                break;

            case SubmitCode submitCode:
                writer.Write(submitCode.Code.TruncateIfNeeded());
                break;

            case UpdateDisplayedValue updateDisplayedValue:
                writer.Write('\'');
                writer.Write(updateDisplayedValue.FormattedValue.Value.TruncateIfNeeded());
                writer.Write("' (");
                writer.Write(updateDisplayedValue.FormattedValue.MimeType);
                writer.Write(')');
                writer.AppendProperties(
                    (nameof(updateDisplayedValue.ValueId), updateDisplayedValue.ValueId));
                break;

            // Base command types.
            case LanguageServiceCommand languageServiceCommand:
                writer.Write(languageServiceCommand.Code.TruncateIfNeeded());
                writer.Write(' ');
                writer.Write(languageServiceCommand.LinePosition.ToString());
                break;

            default:
                break;
        }

        writer.AppendProperties(
            ("Token", command.GetOrCreateToken()),
            (nameof(command.TargetKernelName), command.TargetKernelName),
            (nameof(command.DestinationUri), command.DestinationUri?.ToString()));
    }

    private static void AppendLogString(this TextWriter writer, KernelEvent @event)
    {
        writer.Write("⁞Ε⁞ ");
        writer.Write(@event.GetType().Name);
        writer.Write(' ');

        switch (@event)
        {
            case CodeSubmissionReceived codeSubmissionReceived:
                writer.Write(codeSubmissionReceived.Code.TruncateIfNeeded());
                break;

            case CommandFailed commandFailed:
                writer.Write(commandFailed.Message.TruncateIfNeeded());
                break;

            case CompleteCodeSubmissionReceived completeCodeSubmissionReceived:
                writer.Write(completeCodeSubmissionReceived.Code.TruncateIfNeeded());
                break;

            case CompletionsProduced completionsProduced:
                writer.Write(completionsProduced.LinePositionSpan?.ToString());
                break;

            case DiagnosticsProduced diagnosticsProduced:
                var diagnostics = diagnosticsProduced.Diagnostics;
                var firstMessage = diagnostics.First().Message.TruncateIfNeeded();
                writer.Write(firstMessage);

                var diagnosticsCount = diagnostics.Count;
                if (diagnosticsCount > 1)
                {
                    writer.Write(" (and ");
                    writer.Write(diagnosticsCount - 1);
                    writer.Write(" more)");
                }
                break;

            case ErrorProduced errorProduced:
                writer.Write(errorProduced.Message.TruncateIfNeeded());
                break;

            case HoverTextProduced hoverTextProduced:
                var content = hoverTextProduced.Content;
                var firstContent = content.First();
                writer.Write('\'');
                writer.Write(firstContent.Value.TruncateIfNeeded());
                writer.Write("' (");
                writer.Write(firstContent.MimeType);
                writer.Write(')');

                var contentCount = content.Count;
                if (contentCount > 1)
                {
                    writer.Write(" (and ");
                    writer.Write(contentCount - 1);
                    writer.Write(" more)");
                }
                break;

            case InputProduced inputProduced:
                writer.Write(inputProduced.Value.TruncateIfNeeded());
                break;

            case KernelInfoProduced kernelInfoProduced:
                writer.AppendProperties(
                    (nameof(kernelInfoProduced.KernelInfo.LocalName), kernelInfoProduced.KernelInfo.LocalName),
                    (nameof(kernelInfoProduced.KernelInfo.Uri), kernelInfoProduced.KernelInfo.Uri.ToString()),
                    (nameof(kernelInfoProduced.KernelInfo.IsComposite), kernelInfoProduced.KernelInfo.IsComposite.ToString()),
                    (nameof(kernelInfoProduced.KernelInfo.IsProxy), kernelInfoProduced.KernelInfo.IsProxy.ToString()),
                    (nameof(kernelInfoProduced.KernelInfo.RemoteUri), kernelInfoProduced.KernelInfo.RemoteUri?.ToString()));
                break;

            case KernelReady kernelReady:
                writer.Write('(');
                writer.Write(kernelReady.KernelInfos.Length);
                writer.Write(" Kernels)");
                break;

            case PackageAdded packageAdded:
                writer.Write(packageAdded.PackageReference.ToString());
                break;

            case SignatureHelpProduced signatureHelpProduced:
                writer.Write('(');
                writer.Write(signatureHelpProduced.Signatures?.Count ?? 0);
                writer.Write(" Signatures)");
                writer.AppendProperties(
                    (nameof(signatureHelpProduced.ActiveSignatureIndex), signatureHelpProduced.ActiveSignatureIndex.ToString()),
                    (nameof(signatureHelpProduced.ActiveParameterIndex), signatureHelpProduced.ActiveParameterIndex.ToString()));
                break;

            case ValueInfosProduced valueInfosProduced:
                writer.Write('(');
                writer.Write(valueInfosProduced.ValueInfos.Count);
                writer.Write(" Values)");
                break;

            case ValueProduced valueProduced:
                writer.Write(valueProduced.Name);
                writer.Write(" '");
                writer.Write(valueProduced.FormattedValue.Value.TruncateIfNeeded());
                writer.Write("' (");
                writer.Write(valueProduced.FormattedValue.MimeType);
                writer.Write(')');
                break;

            case WorkingDirectoryChanged workingDirectoryChanged:
                writer.Write(workingDirectoryChanged.WorkingDirectory.TruncateIfNeeded());
                break;

            // Base event types.
            case DisplayEvent displayEvent:
                writer.Write('\'');
                writer.Write(displayEvent.Value?.ToString().TruncateIfNeeded());
                writer.Write('\'');

                var formattedValues = displayEvent.FormattedValues;
                if (formattedValues.Any())
                {
                    writer.Write(" (");
                    writer.Write(nameof(displayEvent.FormattedValues));
                    writer.Write(": ");

                    var firstFormattedValue = formattedValues.First();
                    writer.Write('\'');
                    writer.Write(firstFormattedValue.Value.TruncateIfNeeded());
                    writer.Write("' (");
                    writer.Write(firstFormattedValue.MimeType);
                    writer.Write(')');

                    var formattedValuesCount = formattedValues.Count;
                    if (formattedValuesCount > 1)
                    {
                        writer.Write(" (and ");
                        writer.Write(formattedValuesCount - 1);
                        writer.Write(" more)");
                    }

                    writer.Write(')');
                }

                writer.AppendProperties(
                    (nameof(displayEvent.ValueId), displayEvent.ValueId));
                break;

            default:
                break;
        }

        writer.AppendProperties(
            (nameof(@event.Command), @event.Command.GetType().Name),
            ("CommandToken", @event.Command.GetOrCreateToken()));
    }

    private static void AppendProperties(this TextWriter writer, params (string Name, string? Value)[] properties)
    {
        if (properties.Length > 0)
        {
            writer.Write(" (");

            var i = 0;
            foreach (var property in properties)
            {
                writer.AppendProperty(property.Name, property.Value);

                if (++i != properties.Length)
                {
                    writer.Write(", ");
                }
            }

            writer.Write(')');
        }
    }

    private static void AppendProperty(this TextWriter writer, (string Name, string? Value) property)
        => writer.AppendProperty(property.Name, property.Value);

    private static void AppendProperty(this TextWriter writer, string name, string? value)
    {
        writer.Write(name);
        writer.Write(": ");
        writer.Write(value);
    }

    internal static string TruncateIfNeeded(this string? value, int maxLength = 50)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var lines = value!.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var firstLine = lines.FirstOrDefault();

        if (string.IsNullOrEmpty(firstLine))
        {
            return string.Empty;
        }

        if (firstLine.Length > maxLength)
        {
            firstLine = firstLine.Substring(0, maxLength) + " ...";
        }
        else if (lines.Length > 1)
        {
            firstLine += " ...";
        }

        return firstLine;
    }
}
