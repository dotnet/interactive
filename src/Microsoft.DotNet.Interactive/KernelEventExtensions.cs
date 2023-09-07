// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Parsing;
using Pocket;

namespace Microsoft.DotNet.Interactive;

internal static class KernelEventExtensions
{
    public static LinePositionSpan CalculateLineOffsetFromParentCommand(this KernelEvent @event, LinePositionSpan initialRange)
    {
        if (initialRange is null)
        {
            return null;
        }

        var requestCommand = @event.Command as LanguageServiceCommand;
        if (requestCommand?.Parent is LanguageServiceCommand parentRequest)
        {
            var requestPosition = requestCommand.LinePosition;
            var lineOffset = parentRequest.LinePosition.Line - requestPosition.Line;
            return new LinePositionSpan(
                new LinePosition(initialRange.Start.Line + lineOffset, initialRange.Start.Character),
                new LinePosition(initialRange.End.Line + lineOffset, initialRange.End.Character));
        }

        return initialRange;
    }

    public static IReadOnlyCollection<Diagnostic> RemapDiagnosticsFromRequestingCommand(this KernelEvent @event, IReadOnlyCollection<Diagnostic> diagnostics)
    {
        return @event.Command switch
        {
            SubmitCode submitCode
                when submitCode.LanguageNode is { } => submitCode.LanguageNode.RemapDiagnosticsFromLanguageNode(diagnostics),

            RequestDiagnostics requestDiagnostics
                when requestDiagnostics.LanguageNode is { } => requestDiagnostics.LanguageNode.RemapDiagnosticsFromLanguageNode(diagnostics),

            _ => diagnostics // no meaningful remapping can occur
        };
    }

    private static IReadOnlyCollection<Diagnostic> RemapDiagnosticsFromLanguageNode(this LanguageNode languageNode, IReadOnlyCollection<Diagnostic> diagnostics)
    {
        var root = languageNode.SyntaxTree.GetRoot();
        var initialSpan = languageNode.Span;
        var sourceText = SourceText.From(root.Text);
        var codePosition = sourceText.Lines.GetLinePositionSpan(initialSpan);
        return diagnostics.Select(d => d.WithLinePositionSpan(
                new LinePositionSpan(
                    new LinePosition(d.LinePositionSpan.Start.Line + codePosition.Start.Line, d.LinePositionSpan.Start.Character),
                    new LinePosition(d.LinePositionSpan.End.Line + codePosition.Start.Line, d.LinePositionSpan.End.Character))
            )
        ).ToImmutableList();
    }

    internal static void StampRoutingSlipAndLog(this KernelEvent @event, Uri uri)
    {
        if (@event.RoutingSlip.Count == 0)
        {
            // Log detailed event info if this is the first time the routing slip is being updated.
            Logger.Log.Info("{event}", @event);
        }

        @event.RoutingSlip.Stamp(uri);
        Logger.Log.RoutingSlipInfo(@event, uri);
    }

    private static void RoutingSlipInfo(this Logger logger, KernelEvent @event, Uri uri, string tag = null)
    {
        if (string.IsNullOrEmpty(tag))
        {
            logger.Info("⬅️ {0} {1}", @event.GetType().Name, uri.ToString());
        }
        else
        {
            logger.Info("⬅️ {0} {1} ({2})", @event.GetType().Name, uri.ToString(), tag);
        }
    }
}