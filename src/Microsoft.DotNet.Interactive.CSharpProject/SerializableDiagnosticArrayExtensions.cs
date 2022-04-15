// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.CSharpProject;

internal static class SerializableDiagnosticArrayExtensions
{
    public static bool ContainsError(this IEnumerable<SerializableDiagnostic> diagnostics)
    {
        return diagnostics.Any(e => e.Severity == DiagnosticSeverity.Error);
    }

    public static string[] GetCompileErrorMessages(this IEnumerable<SerializableDiagnostic> diagnostics)
    {
        return diagnostics?.Where(d => d.Severity == DiagnosticSeverity.Error)
                          .Select(d => d.Message)
                          .ToArray() ?? Array.Empty<string>();
    }
}