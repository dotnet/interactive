﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Extensions;

namespace Microsoft.DotNet.Interactive.Events
{
    public class DiagnosticsProduced : KernelEvent
    {
        private IReadOnlyCollection<Diagnostic> _diagnostics;

        public DiagnosticsProduced(IEnumerable<Diagnostic> diagnostics,
            KernelCommand command,
            IReadOnlyCollection<FormattedValue> formattedDiagnostics = null) : base(command)
        {
            _diagnostics = (diagnostics ?? Array.Empty<Diagnostic>()).ToImmutableList();
            FormattedDiagnostics = formattedDiagnostics ?? Array.Empty<FormattedValue>();
        }

        public IReadOnlyCollection<Diagnostic> Diagnostics => this.RemapDiagnosticsFromRequestingCommand(_diagnostics);

        public IReadOnlyCollection<FormattedValue> FormattedDiagnostics { get; }

        public override string ToString() => $"{GetType().Name}";
    }
}
