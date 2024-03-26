// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.CSharpProject;

using Diagnostic = CodeAnalysis.Diagnostic;

internal static class DiagnosticExtensions
{
    public static bool IsError(this Diagnostic diagnostic)
    {
        return diagnostic.Severity == DiagnosticSeverity.Error;
    }

    public static bool ContainsError(this IEnumerable<Diagnostic> diagnostics)
    {
        return diagnostics.Any(e => e.IsError());
    }

    public static SerializableDiagnostic ToSerializableDiagnostic(
        this Diagnostic diagnostic,
        string message = null,
        BufferId bufferId = null)
    {
        var diagnosticMessage = diagnostic.GetMessage();

        var startPosition = diagnostic.Location.GetLineSpan().Span.Start;

        var diagnosticFilePath = diagnostic?.Location.SourceTree?.FilePath
                                 ?? bufferId?.FileName // F# doesn't have a source tree
                                 ?? diagnostic?.Location.GetLineSpan().Path;

        var location =
            diagnostic.Location != null
                ? $"{diagnosticFilePath}({startPosition.Line + 1},{startPosition.Character + 1}): {GetMessagePrefix()}"
                : null;

        return new SerializableDiagnostic(diagnostic.Location?.SourceSpan.Start ?? throw new ArgumentException(nameof(diagnostic.Location)),
            diagnostic.Location.SourceSpan.End,
            message ?? diagnosticMessage,
            diagnostic.Severity,
            diagnostic.Descriptor.Id,
            bufferId,
            location);

        string GetMessagePrefix()
        {
            string prefix;
            switch (diagnostic.Severity)
            {
                case DiagnosticSeverity.Hidden:
                    prefix = "hidden";
                    break;
                case DiagnosticSeverity.Info:
                    prefix = "info";
                    break;
                case DiagnosticSeverity.Warning:
                    prefix = "warning";
                    break;
                case DiagnosticSeverity.Error:
                    prefix = "error";
                    break;
                default:
                    return null;
            }

            return $"{prefix} {diagnostic.Id}";
        }
    }
}