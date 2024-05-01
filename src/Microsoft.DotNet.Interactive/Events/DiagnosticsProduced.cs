// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events;

public class DiagnosticsProduced : KernelEvent
{
    public DiagnosticsProduced(
        IReadOnlyCollection<Diagnostic> diagnostics,
        IReadOnlyCollection<FormattedValue> formattedDiagnostics,
        KernelCommand command) : base(command)
    {
        if (diagnostics is null)
        {
            throw new ArgumentNullException(nameof(diagnostics));
        }

        if (formattedDiagnostics is null)
        {
            throw new ArgumentNullException(nameof(formattedDiagnostics));
        }

        Diagnostics = this.RemapDiagnosticsFromRequestingCommand(diagnostics);

        FormattedDiagnostics = formattedDiagnostics;
    }

    public IReadOnlyCollection<Diagnostic> Diagnostics { get; }

    public IReadOnlyCollection<FormattedValue> FormattedDiagnostics { get; }

    public override string ToString() =>
        $"{nameof(DiagnosticsProduced)}: {string.Join(Environment.NewLine, Diagnostics).TruncateForDisplay()}";
}