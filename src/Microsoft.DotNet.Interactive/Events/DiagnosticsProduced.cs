// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Events;

public class DiagnosticsProduced : KernelEvent
{
    private readonly IReadOnlyCollection<Diagnostic> _diagnostics;

    public DiagnosticsProduced(IEnumerable<Diagnostic> diagnostics,
        KernelCommand command,
        IReadOnlyCollection<FormattedValue> formattedDiagnostics = null) : base(command)
    {
        if (diagnostics is null)
        {
            throw new ArgumentNullException(nameof(diagnostics));
        }
        else if (!diagnostics.Any())
        {
            throw new ArgumentException("At least one diagnostic required.", nameof(diagnostics));
        }

        _diagnostics = diagnostics.ToImmutableList();
        FormattedDiagnostics = formattedDiagnostics ?? Array.Empty<FormattedValue>();
    }

    public IReadOnlyCollection<Diagnostic> Diagnostics => this.RemapDiagnosticsFromRequestingCommand(_diagnostics);

    public IReadOnlyCollection<FormattedValue> FormattedDiagnostics { get; }

    public override string ToString() => $"{GetType().Name}";
}